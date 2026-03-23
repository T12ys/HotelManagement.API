using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelWebApplication.Common.Extensions;
using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.RoomDTOs;
using HotelWebApplication.Enums;
using HotelWebApplication.Models;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelWebApplication.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly HotelDbContext _db;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditLogService _audit;

    public RoomTypeService(
        HotelDbContext db,
        IMapper mapper,
        IFileStorageService fileStorage,
        IAuditLogService audit)
    {
        _db = db;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _audit = audit;
    }

    // READ

    public async Task<PagedResult<RoomTypeResponseDto>> GetPagedAsync(RoomTypeFilterRequest request, CancellationToken ct = default)
    {
        var query = _db.RoomTypes
            .Include(x => x.Photos)
            .Include(x => x.Tags)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Code.ToLower().Contains(s) ||
                x.Description.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(x => x.Code == request.Code);

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive);

        if (request.MinCapacity.HasValue)
            query = query.Where(x => x.Capacity >= request.MinCapacity);

        if (request.MaxCapacity.HasValue)
            query = query.Where(x => x.Capacity <= request.MaxCapacity);

        if (request.MinAdults.HasValue)
            query = query.Where(x => x.MaxOccupancyAdults >= request.MinAdults);

        if (request.MaxAdults.HasValue)
            query = query.Where(x => x.MaxOccupancyAdults <= request.MaxAdults);

        if (request.MinChildren.HasValue)
            query = query.Where(x => x.MaxOccupancyChildren >= request.MinChildren);

        if (request.MaxChildren.HasValue)
            query = query.Where(x => x.MaxOccupancyChildren <= request.MaxChildren);

        if (request.MinPrice.HasValue)
            query = query.Where(x => x.BasePrice >= request.MinPrice);

        if (request.MaxPrice.HasValue)
            query = query.Where(x => x.BasePrice <= request.MaxPrice);

        if (request.TagIds?.Any() == true)
            query = query.Where(x => x.Tags.Any(t => request.TagIds.Contains(t.Id)));

        if (request.CheckIn.HasValue && request.CheckOut.HasValue)
        {
            var checkIn = request.CheckIn.Value.Date;
            var checkOut = request.CheckOut.Value.Date;

            query = query.Where(rt =>
                rt.Rooms.Any(room =>
                    room.IsAvailable &&
                    !_db.Reservations.Any(res =>
                        res.RoomId == room.Id &&
                        (res.Status == ReservationStatus.Pending ||
                         res.Status == ReservationStatus.Confirmed) &&
                        res.StartDate < checkOut &&
                        res.EndDate > checkIn)));
        }

        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<RoomTypeResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<RoomTypeResponseDto>(items, total, request.Page, request.PageSize);
    }

    public async Task<RoomTypeResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.RoomTypes
            .Include(x => x.Photos)
            .Include(x => x.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity == null ? null : _mapper.Map<RoomTypeResponseDto>(entity);
    }

    public async Task<PagedResult<RoomResponseDto>> GetRoomsByTypeIdAsync(
        int roomTypeId, PagedRequest request, CancellationToken ct = default)
    {
        var roomTypeExists = await _db.RoomTypes.AnyAsync(x => x.Id == roomTypeId, ct);
        if (!roomTypeExists)
            throw new KeyNotFoundException("RoomType not found");

        var query = _db.Rooms
            .Where(x => x.RoomTypeId == roomTypeId)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x => x.Number.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy ?? "Number:asc");

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<RoomResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<RoomResponseDto>(items, total, request.Page, request.PageSize);
    }

    // ADMIN

    public async Task<int> CreateAsync(
        CreateRoomTypeDto dto,
        IEnumerable<IFormFile>? photos,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = _mapper.Map<RoomType>(dto);

        if (dto.TagIds?.Any() == true)
        {
            var tags = await _db.Tags
                .Where(t => dto.TagIds.Contains(t.Id))
                .ToListAsync(ct);

            foreach (var tag in tags)
                entity.Tags.Add(tag);
        }

        if (photos?.Any() == true)
        {
            int sort = 0;
            foreach (var file in photos)
            {
                var url = await _fileStorage.SaveFileAsync(file, ct);
                entity.Photos.Add(new RoomPhoto { Url = url, SortOrder = sort++ });
            }
        }

        entity.Capacity = entity.MaxOccupancyAdults + entity.MaxOccupancyChildren;

        _db.RoomTypes.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Create",
            entityType: "RoomType",
            entityId: entity.Id.ToString(),
            newValue: JsonSerializer.Serialize(new
            {
                entity.Code,
                entity.Name,
                entity.BasePrice,
                entity.Capacity,
                entity.IsActive
            }),
            actorUserId: actorUserId,
            ip: ip);

        return entity.Id;
    }

    // Overload for backward compatibility (call from controller without actor)
    public Task<int> CreateAsync(CreateRoomTypeDto dto, IEnumerable<IFormFile>? photos, CancellationToken ct = default)
        => CreateAsync(dto, photos, ct, null, null);

    public async Task UpdateAsync(
        int id,
        UpdateRoomTypeDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.RoomTypes
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("RoomType not found");

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            entity.Code,
            entity.Name,
            entity.BasePrice,
            entity.Capacity,
            entity.IsActive
        });

        _mapper.Map(dto, entity);
        entity.Tags.Clear();

        if (dto.TagIds?.Any() == true)
        {
            var tags = await _db.Tags
                .Where(t => dto.TagIds.Contains(t.Id))
                .ToListAsync(ct);

            foreach (var tag in tags)
                entity.Tags.Add(tag);
        }

        entity.Capacity = entity.MaxOccupancyAdults + entity.MaxOccupancyChildren;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Update",
            entityType: "RoomType",
            entityId: id.ToString(),
            oldValue: oldSnapshot,
            newValue: JsonSerializer.Serialize(new
            {
                entity.Code,
                entity.Name,
                entity.BasePrice,
                entity.Capacity,
                entity.IsActive
            }),
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task UpdateAsync(int id, UpdateRoomTypeDto dto, CancellationToken ct = default)
        => UpdateAsync(id, dto, ct, null, null);

    public async Task DeleteAsync(
        int id,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.RoomTypes
            .Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("RoomType not found");

        var snapshot = JsonSerializer.Serialize(new { entity.Code, entity.Name });

        foreach (var photo in entity.Photos)
            await _fileStorage.DeleteFileAsync(photo.Url, ct);

        _db.RoomTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Delete",
            entityType: "RoomType",
            entityId: id.ToString(),
            oldValue: snapshot,
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => DeleteAsync(id, ct, null, null);

    // PHOTOS

    public async Task AddPhotosAsync(
        int roomTypeId,
        IEnumerable<IFormFile> photos,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.RoomTypes
            .FirstOrDefaultAsync(x => x.Id == roomTypeId, ct)
            ?? throw new KeyNotFoundException("RoomType not found");

        int sort = 0;
        var addedUrls = new List<string>();

        foreach (var file in photos)
        {
            var url = await _fileStorage.SaveFileAsync(file, ct);
            entity.Photos.Add(new RoomPhoto { Url = url, SortOrder = sort++ });
            addedUrls.Add(url);
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "AddPhotos",
            entityType: "RoomType",
            entityId: roomTypeId.ToString(),
            newValue: JsonSerializer.Serialize(new { Count = addedUrls.Count }),
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task AddPhotosAsync(int roomTypeId, IEnumerable<IFormFile> photos, CancellationToken ct = default)
        => AddPhotosAsync(roomTypeId, photos, ct, null, null);

    public async Task DeletePhotoAsync(
        int photoId,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var photo = await _db.RoomPhotos.FirstOrDefaultAsync(x => x.Id == photoId, ct);
        if (photo == null) return;

        var roomTypeId = photo.RoomTypeId;
        await _fileStorage.DeleteFileAsync(photo.Url, ct);
        _db.RoomPhotos.Remove(photo);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "DeletePhoto",
            entityType: "RoomType",
            entityId: roomTypeId.ToString(),
            oldValue: JsonSerializer.Serialize(new { PhotoId = photoId, photo.Url }),
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task DeletePhotoAsync(int photoId, CancellationToken ct = default)
        => DeletePhotoAsync(photoId, ct, null, null);
}