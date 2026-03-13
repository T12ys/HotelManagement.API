using System.Text.Json;
using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.ReservationDTOs;
using HotelWebApplication.Enums;
using HotelWebApplication.Models;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Services;

public class ReservationService : IReservationService
{
    private readonly HotelDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly IPriceRuleService _priceRules;

    private const int HoldMinutes = 15;

    public ReservationService(
        HotelDbContext db,
        IAuditLogService audit,
        IPriceRuleService priceRules)
    {
        _db = db;
        _audit = audit;
        _priceRules = priceRules;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CREATE  (атомарная проверка + hold)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, string? ip = null)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Снимаем просроченные hold-брони перед проверкой
            await ExpireHeldReservationsAsync();

            // 2. Найти свободную комнату нужного типа внутри транзакции
            var room = await _db.Rooms
                .Include(r => r.RoomType)
                .Where(r => r.RoomTypeId == dto.RoomTypeId && r.IsAvailable)
                .Where(r => !_db.Reservations.Any(res =>
                    res.RoomId == r.Id &&
                    (res.Status == ReservationStatus.Pending ||
                     res.Status == ReservationStatus.Confirmed) &&
                    res.StartDate < dto.EndDate &&
                    res.EndDate > dto.StartDate))
                .FirstOrDefaultAsync()
                ?? throw new InvalidOperationException("CONFLICT");

            // 3. Рассчитать итоговую цену
            decimal totalPrice = await CalculateTotalPriceAsync(room, dto.StartDate, dto.EndDate);

            if (dto.Items is { Count: > 0 })
                totalPrice += dto.Items.Sum(i => i.Price * i.Quantity);

            // 4. Создать бронь с найденной комнатой
            var reservation = new Reservation
            {
                RoomId = room.Id,
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerPhone = dto.CustomerPhone,
                StartDate = dto.StartDate.Date,
                EndDate = dto.EndDate.Date,
                TotalPrice = totalPrice,
                Status = ReservationStatus.Pending,
                HeldUntil = DateTime.UtcNow.AddMinutes(HoldMinutes),
                Notes = dto.Notes,
                Source = "web",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (dto.Items is { Count: > 0 })
            {
                reservation.ReservationItems = dto.Items.Select(i => new ReservationItem
                {
                    Name = i.Name,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList();
            }

            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // 5. Audit log
            await _audit.LogAsync(
                actionType: "Create",
                entityType: "Reservation",
                entityId: reservation.Id.ToString(),
                newValue: JsonSerializer.Serialize(new
                {
                    reservation.RoomId,
                    reservation.CustomerEmail,
                    reservation.StartDate,
                    reservation.EndDate,
                    reservation.TotalPrice,
                    reservation.Status
                }),
                ip: ip);

            return await MapToResponseAsync(reservation, room);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET BY ID
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ReservationResponseDto?> GetByIdAsync(Guid id)
    {
        var r = await _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        return r is null ? null : MapToResponse(r);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET ALL (admin)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<PagedResult<ReservationResponseDto>> GetAllAsync(ReservationFilterRequest filter)
    {
        var query = _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.ReservationItems)
            .AsQueryable();

        if (filter.RoomTypeId.HasValue)
            query = query.Where(r => r.Room.RoomTypeId == filter.RoomTypeId.Value);

        if (filter.RoomId.HasValue)
            query = query.Where(r => r.RoomId == filter.RoomId.Value);

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);

        if (filter.From.HasValue)
            query = query.Where(r => r.EndDate > filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(r => r.StartDate < filter.To.Value);

        if (!string.IsNullOrWhiteSpace(filter.CustomerEmail))
            query = query.Where(r => r.CustomerEmail.Contains(filter.CustomerEmail));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ReservationResponseDto>(
            items.Select(MapToResponse),
            total,
            filter.Page,
            filter.PageSize);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UPDATE (admin/moderator)
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ReservationResponseDto> UpdateAsync(
        Guid id, UpdateReservationDto dto, Guid actorUserId, string? ip = null)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Reservation {id} not found.");

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            reservation.StartDate,
            reservation.EndDate,
            reservation.Status,
            reservation.Notes
        });

        bool datesChanged =
            (dto.StartDate.HasValue && dto.StartDate.Value.Date != reservation.StartDate) ||
            (dto.EndDate.HasValue && dto.EndDate.Value.Date != reservation.EndDate);

        if (datesChanged)
        {
            var newStart = dto.StartDate?.Date ?? reservation.StartDate;
            var newEnd = dto.EndDate?.Date ?? reservation.EndDate;

            var conflict = await _db.Reservations.AnyAsync(r =>
                r.Id != id &&
                r.RoomId == reservation.RoomId &&
                (r.Status == ReservationStatus.Pending ||
                 r.Status == ReservationStatus.Confirmed) &&
                r.StartDate < newEnd &&
                r.EndDate > newStart);

            if (conflict)
                throw new InvalidOperationException("CONFLICT");

            reservation.StartDate = newStart;
            reservation.EndDate = newEnd;

            reservation.TotalPrice = await CalculateTotalPriceAsync(reservation.Room, newStart, newEnd);
            reservation.TotalPrice += reservation.ReservationItems.Sum(i => i.Price * i.Quantity);
        }

        if (dto.Status.HasValue)
            reservation.Status = dto.Status.Value;

        if (dto.Notes is not null)
            reservation.Notes = dto.Notes;

        reservation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            actionType: "Update",
            entityType: "Reservation",
            entityId: id.ToString(),
            oldValue: oldSnapshot,
            newValue: JsonSerializer.Serialize(new
            {
                reservation.StartDate,
                reservation.EndDate,
                reservation.Status,
                reservation.Notes
            }),
            actorUserId: actorUserId,
            ip: ip);

        return MapToResponse(reservation);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CANCEL
    // ─────────────────────────────────────────────────────────────────────────
    public async Task CancelAsync(Guid id, Guid actorUserId, string? ip = null)
    {
        var reservation = await _db.Reservations.FindAsync(id)
            ?? throw new KeyNotFoundException($"Reservation {id} not found.");

        if (reservation.Status == ReservationStatus.Cancelled)
            return;

        var oldStatus = reservation.Status.ToString();

        reservation.Status = ReservationStatus.Cancelled;
        reservation.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            actionType: "Cancel",
            entityType: "Reservation",
            entityId: id.ToString(),
            oldValue: oldStatus,
            newValue: ReservationStatus.Cancelled.ToString(),
            actorUserId: actorUserId,
            ip: ip);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MOCK PAYMENT
    // ─────────────────────────────────────────────────────────────────────────
    public async Task<ReservationResponseDto> ProcessMockPaymentAsync(
        Guid reservationId, bool simulateSuccess, string? ip = null)
    {
        var reservation = await _db.Reservations
            .Include(r => r.Room)
            .Include(r => r.ReservationItems)
            .FirstOrDefaultAsync(r => r.Id == reservationId)
            ?? throw new KeyNotFoundException($"Reservation {reservationId} not found.");

        if (reservation.Status != ReservationStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot process payment for reservation in status '{reservation.Status}'. Expected 'Pending'.");

        if (reservation.HeldUntil.HasValue && reservation.HeldUntil.Value < DateTime.UtcNow)
        {
            reservation.Status = ReservationStatus.Cancelled;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync("HoldExpired", "Reservation", reservationId.ToString(), ip: ip);

            throw new InvalidOperationException("HOLD_EXPIRED");
        }

        if (simulateSuccess)
        {
            reservation.Status = ReservationStatus.Confirmed;
            reservation.PaidAt = DateTime.UtcNow;
            reservation.HeldUntil = null;
            reservation.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                actionType: "PaymentConfirmed",
                entityType: "Reservation",
                entityId: reservationId.ToString(),
                newValue: JsonSerializer.Serialize(new
                {
                    reservation.Status,
                    reservation.PaidAt
                }),
                ip: ip);
        }
        else
        {
            await _audit.LogAsync(
                actionType: "PaymentFailed",
                entityType: "Reservation",
                entityId: reservationId.ToString(),
                ip: ip);

            throw new InvalidOperationException("PAYMENT_FAILED");
        }

        return MapToResponse(reservation);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private async Task ExpireHeldReservationsAsync()
    {
        var expired = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Pending &&
                        r.HeldUntil.HasValue &&
                        r.HeldUntil.Value < DateTime.UtcNow)
            .ToListAsync();

        foreach (var r in expired)
        {
            r.Status = ReservationStatus.Cancelled;
            r.UpdatedAt = DateTime.UtcNow;
        }

        if (expired.Count > 0)
            await _db.SaveChangesAsync();
    }

    private async Task<decimal> CalculateTotalPriceAsync(Room room, DateTime start, DateTime end)
    {
        var nights = (int)(end.Date - start.Date).TotalDays;
        if (nights <= 0) nights = 1;

        try
        {
            var priceResponse = await _priceRules.CalculatePriceAsync(
                new DTOs.PriceDTOs.PriceCalculationRequestDto
                {
                    RoomTypeId = room.RoomTypeId,
                    StartDate = start.Date,
                    EndDate = end.Date
                });

            return priceResponse.FinalTotalPrice;
        }
        catch
        {
            var basePrice = room.RoomType?.BasePrice ?? 0m;
            return basePrice * nights;
        }
    }

    private static ReservationResponseDto MapToResponse(Reservation r)
    {
        var nights = (int)(r.EndDate - r.StartDate).TotalDays;

        return new ReservationResponseDto
        {
            Id = r.Id,
            RoomId = r.RoomId,
            RoomNumber = r.Room?.Number ?? r.RoomId.ToString(),
            CustomerName = r.CustomerName,
            CustomerEmail = r.CustomerEmail,
            CustomerPhone = r.CustomerPhone,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            NightsCount = nights > 0 ? nights : 1,
            TotalPrice = r.TotalPrice,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
            PaidAt = r.PaidAt,
            HeldUntil = r.HeldUntil,
            Notes = r.Notes,
            Source = r.Source,
            Items = r.ReservationItems.Select(i => new ReservationItemResponseDto
            {
                Id = i.Id,
                Name = i.Name,
                Price = i.Price,
                Quantity = i.Quantity,
                Total = i.Price * i.Quantity
            }).ToList()
        };
    }

    private static Task<ReservationResponseDto> MapToResponseAsync(Reservation r, Room room)
    {
        r.Room = room;
        return Task.FromResult(MapToResponse(r));
    }
}