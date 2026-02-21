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

public class RoomTypeService : IRoomTypeService
{
    private readonly HotelDbContext _db;
    private readonly IMapper _mapper;
    private readonly IFileStorageService _fileStorage;

    public RoomTypeService(HotelDbContext db,IMapper mapper, IFileStorageService fileStorage)
    {
        _db = db;
        _mapper = mapper;
        _fileStorage = fileStorage;
    }

    // READ

    public async Task<PagedResult<RoomTypeResponseDto>> GetPagedAsync(RoomTypeFilterRequest request, CancellationToken ct = default)
    {
        var query = _db.RoomTypes
            .Include(x => x.Photos)
            .Include(x => x.Tags)
            .AsNoTracking()
            .AsQueryable();

        // 🔎 Text search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Code.ToLower().Contains(s) ||
                x.Description.ToLower().Contains(s));
        }

        // Code
        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(x => x.Code == request.Code);

        // IsActive
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive);

        // Capacity
        if (request.MinCapacity.HasValue)
            query = query.Where(x => x.Capacity >= request.MinCapacity);

        if (request.MaxCapacity.HasValue)
            query = query.Where(x => x.Capacity <= request.MaxCapacity);

        // Adults
        if (request.MinAdults.HasValue)
            query = query.Where(x => x.MaxOccupancyAdults >= request.MinAdults);

        if (request.MaxAdults.HasValue)
            query = query.Where(x => x.MaxOccupancyAdults <= request.MaxAdults);

        // Children
        if (request.MinChildren.HasValue)
            query = query.Where(x => x.MaxOccupancyChildren >= request.MinChildren);

        if (request.MaxChildren.HasValue)
            query = query.Where(x => x.MaxOccupancyChildren <= request.MaxChildren);

        // Price
        if (request.MinPrice.HasValue)
            query = query.Where(x => x.BasePrice >= request.MinPrice);

        if (request.MaxPrice.HasValue)
            query = query.Where(x => x.BasePrice <= request.MaxPrice);

        // Tags
        if (request.TagIds?.Any() == true)
        {
            query = query.Where(x =>
                x.Tags.Any(t => request.TagIds.Contains(t.Id)));
        }

        // Sorting
        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<RoomTypeResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<RoomTypeResponseDto>(
            items,
            total,
            request.Page,
            request.PageSize);
    }

    public async Task<RoomTypeResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.RoomTypes
            .Include(x => x.Photos)
            .Include(x => x.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity == null
            ? null
            : _mapper.Map<RoomTypeResponseDto>(entity);
    }

    // =========================
    // ADMIN
    // =========================

    public async Task<int> CreateAsync(CreateRoomTypeDto dto, IEnumerable<IFormFile>? photos, CancellationToken ct = default)
    {
        var entity = _mapper.Map<RoomType>(dto);

        // Tags
        if (dto.TagIds?.Any() == true)
        {
            var tags = await _db.Tags
                .Where(t => dto.TagIds.Contains(t.Id))
                .ToListAsync(ct);

            foreach (var tag in tags)
                entity.Tags.Add(tag);
        }

        // Photos
        if (photos?.Any() == true)
        {
            int sort = 0;

            foreach (var file in photos)
            {
                var url = await _fileStorage.SaveFileAsync(file, ct);

                entity.Photos.Add(new RoomPhoto
                {
                    Url = url,
                    SortOrder = sort++
                });
            }
        }

        _db.RoomTypes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }

    public async Task UpdateAsync(int id,UpdateRoomTypeDto dto, CancellationToken ct = default)
    {
        var entity = await _db.RoomTypes
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity == null)
            throw new KeyNotFoundException("RoomType not found");

        _mapper.Map(dto, entity);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.RoomTypes
            .Include(x => x.Photos)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity == null)
            throw new KeyNotFoundException("RoomType not found");

        foreach (var photo in entity.Photos)
            await _fileStorage.DeleteFileAsync(photo.Url, ct);

        _db.RoomTypes.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    // =========================
    // MODERATOR - PHOTOS
    // =========================

    public async Task AddPhotosAsync(int roomTypeId,IEnumerable<IFormFile> photos, CancellationToken ct = default)
    {
        var entity = await _db.RoomTypes
            .FirstOrDefaultAsync(x => x.Id == roomTypeId, ct) ?? throw new KeyNotFoundException("RoomType not found");
        int sort = 0;

        foreach (var file in photos)
        {
            var url = await _fileStorage.SaveFileAsync(file, ct);

            entity.Photos.Add(new RoomPhoto
            {
                Url = url,
                SortOrder = sort++
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeletePhotoAsync(int photoId, CancellationToken ct = default)
    {
        var photo = await _db.RoomPhotos
            .FirstOrDefaultAsync(x => x.Id == photoId, ct);

        if (photo == null)
            return;

        await _fileStorage.DeleteFileAsync(photo.Url, ct);

        _db.RoomPhotos.Remove(photo);
        await _db.SaveChangesAsync(ct);
    }
}