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

public class TagService : ITagService
{
    private readonly HotelDbContext _db;
    private readonly IMapper _mapper;
    private readonly IConfiguration _cfg;
    private readonly IAuditLogService _audit;

    public TagService(HotelDbContext db, IMapper mapper, IConfiguration cfg, IAuditLogService audit)
    {
        _db = db;
        _mapper = mapper;
        _cfg = cfg;
        _audit = audit;
    }

    // READ

    private static string Slugify(string input)
    {
        return System.Text.RegularExpressions.Regex
            .Replace(input.ToLower().Trim(), @"[^a-z0-9]+", "-")
            .Trim('-');
    }

    public async Task<PagedResult<TagResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Tags.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x => x.Slug.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<TagResponseDto>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<TagResponseDto>(items, total, request.Page, request.PageSize);
    }

    public async Task<TagResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Tags.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return entity == null ? null : _mapper.Map<TagResponseDto>(entity);
    }

    // WRITE

    public async Task<int> CreateAsync(
        CreateTagDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        if (!dto.Translations.ContainsKey("en") || string.IsNullOrWhiteSpace(dto.Translations["en"]))
            throw new InvalidOperationException("English translation is required.");

        var slug = Slugify(dto.Translations["en"]);

        var exists = await _db.Tags.AnyAsync(x => x.Slug == slug, ct);
        if (exists)
            throw new InvalidOperationException("Tag with this name already exists.");

        var supportedLangs = _cfg.GetSection("SupportedLanguages").Get<string[]>() ?? [];
        var filteredTranslations = dto.Translations
            .Where(kv => supportedLangs.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var entity = new Tag { Slug = slug, Translations = filteredTranslations };

        _db.Tags.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Create",
            entityType: "Tag",
            entityId: entity.Id.ToString(),
            newValue: JsonSerializer.Serialize(new { entity.Slug, entity.Translations }),
            actorUserId: actorUserId,
            ip: ip);

        return entity.Id;
    }

    public Task<int> CreateAsync(CreateTagDto dto, CancellationToken ct = default)
        => CreateAsync(dto, ct, null, null);

    public async Task UpdateAsync(
        int id,
        CreateTagDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Tag not found");

        if (!dto.Translations.ContainsKey("en") || string.IsNullOrWhiteSpace(dto.Translations["en"]))
            throw new InvalidOperationException("English translation is required.");

        var oldSnapshot = JsonSerializer.Serialize(new { entity.Slug, entity.Translations });

        var supportedLangs = _cfg.GetSection("SupportedLanguages").Get<string[]>() ?? [];
        var filteredTranslations = dto.Translations
            .Where(kv => supportedLangs.Contains(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        entity.Translations = filteredTranslations;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Update",
            entityType: "Tag",
            entityId: id.ToString(),
            oldValue: oldSnapshot,
            newValue: JsonSerializer.Serialize(new { entity.Slug, entity.Translations }),
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task UpdateAsync(int id, CreateTagDto dto, CancellationToken ct = default)
        => UpdateAsync(id, dto, ct, null, null);

    public async Task DeleteAsync(
        int id,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException("Tag not found");

        var snapshot = JsonSerializer.Serialize(new { entity.Slug });

        _db.Tags.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "Delete",
            entityType: "Tag",
            entityId: id.ToString(),
            oldValue: snapshot,
            actorUserId: actorUserId,
            ip: ip);
    }

    public Task DeleteAsync(int id, CancellationToken ct = default)
        => DeleteAsync(id, ct, null, null);
}