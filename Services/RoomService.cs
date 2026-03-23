using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelWebApplication.Common.Extensions;
using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Models;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelWebApplication.Services;

public class RoomService : IRoomService
{
    private readonly HotelDbContext _db;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _audit;

    public RoomService(HotelDbContext db, IMapper mapper, IAuditLogService audit)
    {
        _db = db;
        _mapper = mapper;
        _audit = audit;
    }

    // READ

    public async Task<PagedResult<RoomResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Rooms
            .Include(x => x.RoomType)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x => x.Number.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<RoomResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<RoomResponseDto>(items, total, request.Page, request.PageSize);
    }

    public async Task<RoomResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Rooms
            .Include(x => x.RoomType)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity == null ? null : _mapper.Map<RoomResponseDto>(entity);
    }

    // WRITE

    public async Task<int> CreateAsync(
        CreateRoomDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var roomTypeExists = await _db.RoomTypes.AnyAsync(x => x.Id == dto.RoomTypeId, ct);
        if (!roomTypeExists)
            throw new KeyNotFoundException("RoomType not found");

        var entity = _mapper.Map<Room>(dto);

        _db.Rooms.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Create",
            entityType: "Room",
            entityId: entity.Id.ToString(),
            newValue: JsonSerializer.Serialize(new
            {
                entity.Number,
                entity.RoomTypeId,
                entity.Floor
            }),
            actorUserId: actorUserId,
            ip: ip);

        return entity.Id;
    }

    public Task<int> CreateAsync(CreateRoomDto dto, CancellationToken ct = default)
        => CreateAsync(dto, ct, null, null);

    public async Task UpdateAsync(
        int id,
        UpdateRoomDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Room not found");

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            entity.Number,
            entity.RoomTypeId,
            entity.Floor
        });

        _mapper.Map(dto, entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Update",
            entityType: "Room",
            entityId: id.ToString(),
            oldValue: oldSnapshot,
            newValue: JsonSerializer.Serialize(new
            {
                entity.Number,
                entity.RoomTypeId,
                entity.Floor
            }),
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task UpdateAsync(int id, UpdateRoomDto dto, CancellationToken ct = default)
        => UpdateAsync(id, dto, ct, null, null);

    public async Task ChangeAvailabilityAsync(
        int id,
        ChangeRoomAvailabilityDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Room not found");

        var oldAvailability = entity.IsAvailable;
        entity.IsAvailable = dto.IsAvailable;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "ChangeAvailability",
            entityType: "Room",
            entityId: id.ToString(),
            oldValue: JsonSerializer.Serialize(new { IsAvailable = oldAvailability }),
            newValue: JsonSerializer.Serialize(new { dto.IsAvailable }),
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task ChangeAvailabilityAsync(int id, ChangeRoomAvailabilityDto dto, CancellationToken ct = default)
        => ChangeAvailabilityAsync(id, dto, ct, null, null);

    // DELETE

    public async Task DeleteAsync(
        int id,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.Rooms.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Room not found");

        var snapshot = JsonSerializer.Serialize(new { entity.Number, entity.RoomTypeId });

        _db.Rooms.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Delete",
            entityType: "Room",
            entityId: id.ToString(),
            oldValue: snapshot,
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => DeleteAsync(id, ct, null, null);
}