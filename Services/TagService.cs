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

public class TagService : ITagService
{
    private readonly HotelDbContext _db;
    private readonly IMapper _mapper;

    public TagService(HotelDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    // READ

    public async Task<PagedResult<TagResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Tags
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Slug.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<TagResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<TagResponseDto>(
            items,
            total,
            request.Page,
            request.PageSize);
    }

    public async Task<TagResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity == null
            ? null
            : _mapper.Map<TagResponseDto>(entity);
    }

    // WRITE

    public async Task<int> CreateAsync(CreateTagDto dto, CancellationToken ct = default)
    {
        var exists = await _db.Tags
            .AnyAsync(x => x.Slug == dto.Slug, ct);

        if (exists)
            throw new InvalidOperationException("Tag slug must be unique");

        var entity = _mapper.Map<Tag>(dto);

        _db.Tags.Add(entity);
        await _db.SaveChangesAsync(ct);

        return entity.Id;
    }

    public async Task UpdateAsync(int id, CreateTagDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Tags
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity == null)
            throw new KeyNotFoundException("Tag not found");

        var slugExists = await _db.Tags
            .AnyAsync(x => x.Slug == dto.Slug && x.Id != id, ct);

        if (slugExists)
            throw new InvalidOperationException("Tag slug must be unique");

        _mapper.Map(dto, entity);

        await _db.SaveChangesAsync(ct);
    }

    // DELETE (Admin only)

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Tags
            .FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new KeyNotFoundException("Tag not found");
        _db.Tags.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
