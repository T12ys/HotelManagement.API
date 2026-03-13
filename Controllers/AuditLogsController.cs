using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.AuditLogDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Просмотр истории действий (только Admin)
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
    /// Список записей аудита с фильтрацией
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogFilterRequest filter)
    {
        var query = _db.AuditLogs
            .Include(a => a.ActorUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(a => a.EntityId == filter.EntityId);

        if (!string.IsNullOrWhiteSpace(filter.ActionType))
            query = query.Where(a => a.ActionType == filter.ActionType);

        if (filter.ActorUserId.HasValue)
            query = query.Where(a => a.ActorUserId == filter.ActorUserId.Value);

        if (filter.From.HasValue)
            query = query.Where(a => a.Timestamp >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(a => a.Timestamp <= filter.To.Value);

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
                IP = a.IP,
                Timestamp = a.Timestamp
            })
            .ToListAsync();

        return Ok(items);
    }
}