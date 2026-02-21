using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelWebApplication.Common.Extensions;
using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Models;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Services;

public class RoomService : IRoomService
{
    private readonly HotelDbContext _db;
    private readonly IMapper _mapper;

    public RoomService(HotelDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // READ

    public async Task<PagedResult<RoomResponseDto>> GetPagedAsync(PagedRequest request,CancellationToken ct = default)
    {
        var query = _db.Rooms
            .Include(x => x.RoomType)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x =>
                x.Number.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<RoomResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<RoomResponseDto>(
            items,
            total,
            request.Page,
            request.PageSize);
    }

    public async Task<RoomResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Rooms
            .Include(x => x.RoomType)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity == null
            ? null
            : _mapper.Map<RoomResponseDto>(entity);
    }

    // WRITE

    public async Task<int> CreateAsync(CreateRoomDto dto, CancellationToken ct = default)
    {
        // Проверяем существование RoomType
        var roomTypeExists = await _db.RoomTypes
            .AnyAsync(x => x.Id == dto.RoomTypeId, ct);

        if (!roomTypeExists)
            throw new KeyNotFoundException("RoomType not found");

        var entity = _mapper.Map<Room>(dto);

        _db.Rooms.Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }

    public async Task UpdateAsync(int id, UpdateRoomDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Rooms
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity == null)
            throw new KeyNotFoundException("Room not found");

        _mapper.Map(dto, entity);

        await _db.SaveChangesAsync(ct);
    }

    public async Task ChangeAvailabilityAsync(int id, ChangeRoomAvailabilityDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Rooms
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Room not found");
        entity.IsAvailable = dto.IsAvailable;

        await _db.SaveChangesAsync(ct);
    }

    // DELETE (Admin only)

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Rooms
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity == null)
            throw new KeyNotFoundException("Room not found");

        _db.Rooms.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
