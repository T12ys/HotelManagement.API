using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.AuditLogDTOs;
using HotelWebApplication.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Просмотр истории действий пользователей (только Admin)
/// </summary>
[ApiController]
[Route("api/admin/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly HotelDbContext _db;

    public AuditLogsController(HotelDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Список записей аудита с расширенной фильтрацией.
    /// Поддерживает мультивыбор пользователей (?ActorUserIds=guid1&amp;ActorUserIds=guid2),
    /// мультивыбор типов действий (?ActionTypes=Create&amp;ActionTypes=Delete),
    /// фильтрацию по роли актора и диапазону дат.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogFilterRequest filter)
    {
        var query = _db.AuditLogs
            .Include(a => a.ActorUser)
            .AsQueryable();

        // Фильтр по типу сущности
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        // Фильтр по конкретной записи
        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(a => a.EntityId == filter.EntityId);

        // Тип действия: одиночный или мультивыбор
        var actionTypes = BuildStringList(filter.ActionTypes, filter.ActionType);
        if (actionTypes.Count > 0)
            query = query.Where(a => actionTypes.Contains(a.ActionType));

        // Пользователь: одиночный или мультивыбор
        var actorUserIds = BuildGuidList(filter.ActorUserIds, filter.ActorUserId);
        if (actorUserIds.Count > 0)
            query = query.Where(a => a.ActorUserId.HasValue && actorUserIds.Contains(a.ActorUserId.Value));

        // Фильтр по роли (требует join с User)
        if (!string.IsNullOrWhiteSpace(filter.ActorRole) &&
            Enum.TryParse<UserRole>(filter.ActorRole, ignoreCase: true, out var role))
        {
            query = query.Where(a => a.ActorUser != null && a.ActorUser.Role == role);
        }

        // Диапазон дат
        if (filter.From.HasValue)
            query = query.Where(a => a.Timestamp >= filter.From.Value);

        if (filter.To.HasValue)
        {
            // Включаем весь конечный день
            var toEndOfDay = filter.To.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.Timestamp <= toEndOfDay);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogResponseDto
            {
                Id = a.Id,
                ActionType = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ActorUserId = a.ActorUserId,
                ActorName = a.ActorUser != null ? a.ActorUser.DisplayName : null,
                ActorRole = a.ActorUser != null ? a.ActorUser.Role.ToString() : null,
                IP = a.IP,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(new PagedResult<AuditLogResponseDto>(items, total, filter.Page, filter.PageSize));
    }

    /// <summary>
    /// Все логи по конкретной сущности (например все действия с конкретной бронью)
    /// </summary>
    [HttpGet("{entityType}/{entityId}")]
    [ProducesResponseType(typeof(List<AuditLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(string entityType, string entityId)
    {
        var items = await _db.AuditLogs
            .Include(a => a.ActorUser)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogResponseDto
            {
                Id = a.Id,
                ActionType = a.ActionType,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ActorUserId = a.ActorUserId,
                ActorName = a.ActorUser != null ? a.ActorUser.DisplayName : null,
                ActorRole = a.ActorUser != null ? a.ActorUser.Role.ToString() : null,
                IP = a.IP,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Список всех уникальных actionType-ов — для заполнения фильтра на фронте
    /// </summary>
    [HttpGet("action-types")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActionTypes()
    {
        var types = await _db.AuditLogs
            .Select(a => a.ActionType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return Ok(types);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Объединяет список и одиночное значение в единый список без дублей
    /// </summary>
    private static List<string> BuildStringList(List<string>? list, string? single)
    {
        var result = new List<string>();
        if (list?.Count > 0) result.AddRange(list.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(single) && !result.Contains(single)) result.Add(single);
        return result;
    }

    private static List<Guid> BuildGuidList(List<Guid>? list, Guid? single)
    {
        var result = new List<Guid>();
        if (list?.Count > 0) result.AddRange(list);
        if (single.HasValue && !result.Contains(single.Value)) result.Add(single.Value);
        return result;
    }
}