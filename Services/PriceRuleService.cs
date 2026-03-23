using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelWebApplication.Common.Extensions;
using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.PriceDTOs;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Models;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelWebApplication.Services;

public class PriceRuleService : IPriceRuleService
{
    private readonly HotelDbContext _db;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _audit;

    public PriceRuleService(HotelDbContext db, IMapper mapper, IAuditLogService audit)
    {
        _db = db;
        _mapper = mapper;
        _audit = audit;
    }

    // READ

    public async Task<PagedResult<PriceRuleResponseDto>> GetByRoomTypeAsync(int roomTypeId, PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.PriceRules
            .AsNoTracking()
            .Where(x => x.RoomTypeId == roomTypeId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<PriceRuleResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<PriceRuleResponseDto>(items, total, request.Page, request.PageSize);
    }

    public async Task<PagedResult<PriceRuleResponseDto>> GetRulesForPeriodAsync(PeriodRulesRequestDto dto, CancellationToken ct = default)
    {
        var query = _db.PriceRules
            .AsNoTracking()
            .Where(x => x.IsActive
                && (x.RoomTypeId == dto.RoomTypeId || x.RoomTypeId == null)
                && x.StartDate <= dto.To
                && x.EndDate >= dto.From)
            .OrderBy(x => x.StartDate)
            .AsQueryable();

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((dto.Page - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ProjectTo<PriceRuleResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<PriceRuleResponseDto>(items, total, dto.Page, dto.PageSize);
    }

    public async Task<PriceRuleResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var rule = await _db.PriceRules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return rule == null ? null : _mapper.Map<PriceRuleResponseDto>(rule);
    }

    public async Task<PagedResult<PriceRuleResponseDto>> GetAllAsync(int? roomTypeId, PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.PriceRules.AsNoTracking().AsQueryable();

        if (roomTypeId.HasValue)
            query = query.Where(x => x.RoomTypeId == roomTypeId || x.RoomTypeId == null);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<PriceRuleResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<PriceRuleResponseDto>(items, total, request.Page, request.PageSize);
    }

    // WRITE

    public async Task<int> CreateAsync(
        CreatePriceRuleDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        if (dto.RoomTypeId.HasValue)
        {
            var exists = await _db.RoomTypes.AnyAsync(x => x.Id == dto.RoomTypeId.Value, ct);
            if (!exists)
                throw new KeyNotFoundException("RoomType not found.");
        }

        var entity = _mapper.Map<PriceRule>(dto);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _db.PriceRules.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Create",
            entityType: "PriceRule",
            entityId: entity.Id.ToString(),
            newValue: JsonSerializer.Serialize(new
            {
                entity.Name,
                entity.RoomTypeId,
                entity.StartDate,
                entity.EndDate,
                entity.Value,
                entity.IsPercent,
                entity.IsIncrease
            }),
            actorUserId: actorUserId,
            ip: ip);

        return entity.Id;
    }

    public Task<int> CreateAsync(CreatePriceRuleDto dto, CancellationToken ct = default)
        => CreateAsync(dto, ct, null, null);

    public async Task UpdateAsync(
        int id,
        UpdatePriceRuleDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.PriceRules.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("PriceRule not found.");

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            entity.Name,
            entity.StartDate,
            entity.EndDate,
            entity.Value,
            entity.IsPercent,
            entity.IsIncrease,
            entity.IsActive
        });

        _mapper.Map(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Update",
            entityType: "PriceRule",
            entityId: id.ToString(),
            oldValue: oldSnapshot,
            newValue: JsonSerializer.Serialize(new
            {
                entity.Name,
                entity.StartDate,
                entity.EndDate,
                entity.Value,
                entity.IsPercent,
                entity.IsIncrease,
                entity.IsActive
            }),
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task UpdateAsync(int id, UpdatePriceRuleDto dto, CancellationToken ct = default)
        => UpdateAsync(id, dto, ct, null, null);

    public async Task DeleteAsync(
        int id,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.PriceRules.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("PriceRule not found.");

        var snapshot = JsonSerializer.Serialize(new { entity.Name, entity.RoomTypeId });

        _db.PriceRules.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Delete",
            entityType: "PriceRule",
            entityId: id.ToString(),
            oldValue: snapshot,
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => DeleteAsync(id, ct, null, null);

    // CALCULATE

    public async Task<PriceCalculationResponseDto> CalculatePriceAsync(PriceCalculationRequestDto dto, CancellationToken ct = default)
    {
        var roomType = await _db.RoomTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.RoomTypeId, ct)
            ?? throw new KeyNotFoundException("RoomType not found.");

        var rules = await LoadRulesForRangeAsync(dto.RoomTypeId, dto.StartDate, dto.EndDate, ct);

        var basePrice = roomType.BasePrice;
        var nights = (int)(dto.EndDate.Date - dto.StartDate.Date).TotalDays;
        var dailyBreakdown = new List<DailyPriceDto>();

        for (var i = 0; i < nights; i++)
        {
            var date = dto.StartDate.Date.AddDays(i);
            var dayRules = rules.Where(x => x.StartDate.Date <= date && x.EndDate.Date >= date).ToList();

            var dailyPrice = basePrice;
            var appliedRules = new List<AppliedRuleDto>();

            foreach (var rule in dayRules)
            {
                var delta = rule.IsPercent
                    ? Math.Round(basePrice * rule.Value / 100, 2)
                    : rule.Value;

                if (!rule.IsIncrease) delta = -delta;

                dailyPrice += delta;
                appliedRules.Add(new AppliedRuleDto
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    PriceDelta = delta
                });
            }

            var minPrice = basePrice * 0.30m;
            if (dailyPrice < minPrice) dailyPrice = minPrice;

            dailyBreakdown.Add(new DailyPriceDto
            {
                Date = date,
                BasePrice = basePrice,
                FinalPrice = dailyPrice,
                AppliedRules = appliedRules
            });
        }

        return new PriceCalculationResponseDto
        {
            RoomTypeId = dto.RoomTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Nights = nights,
            BaseTotalPrice = basePrice * nights,
            FinalTotalPrice = dailyBreakdown.Sum(x => x.FinalPrice),
            DailyBreakdown = dailyBreakdown
        };
    }

    public async Task<PagedResult<RoomTypeResponseDto>> GetDiscountedRoomTypesAsync(PagedRequest request, CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;

        var roomTypes = await _db.RoomTypes
            .Include(x => x.Photos)
            .Include(x => x.Tags)
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(ct);

        var rules = await _db.PriceRules
            .AsNoTracking()
            .Where(x => x.IsActive && x.StartDate <= today && x.EndDate >= today)
            .ToListAsync(ct);

        var discounted = roomTypes
            .Where(roomType =>
            {
                var basePrice = roomType.BasePrice;
                var threshold = basePrice * 0.85m;

                var roomRules = rules
                    .Where(r => r.RoomTypeId == roomType.Id || r.RoomTypeId == null)
                    .ToList();

                var finalPrice = basePrice;
                foreach (var rule in roomRules)
                {
                    var delta = rule.IsPercent
                        ? Math.Round(basePrice * rule.Value / 100, 2)
                        : rule.Value;

                    if (!rule.IsIncrease) delta = -delta;
                    finalPrice += delta;
                }

                return finalPrice <= threshold;
            })
            .ToList();

        var total = discounted.Count;

        var items = discounted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => _mapper.Map<RoomTypeResponseDto>(x))
            .ToList();

        return new PagedResult<RoomTypeResponseDto>(items, total, request.Page, request.PageSize);
    }

    private async Task<List<PriceRule>> LoadRulesForRangeAsync(int roomTypeId, DateTime from, DateTime to, CancellationToken ct)
    {
        return await _db.PriceRules
            .AsNoTracking()
            .Where(x => x.IsActive
                && (x.RoomTypeId == roomTypeId || x.RoomTypeId == null)
                && x.StartDate <= to
                && x.EndDate >= from)
            .ToListAsync(ct);
    }
}