using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.AuditLogDTOs;
using HotelWebApplication.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Provides read-only access to the audit log.
/// Records are written automatically whenever a reservation, room, user or price rule is created, updated or deleted.
/// Accessible by Admin role only.
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
    /// Returns a paginated and filtered list of audit log entries.
    /// Supports multi-select for users (<c>ActorUserIds</c>) and action types (<c>ActionTypes</c>),
    /// filtering by actor role, entity type, entity Id, and date range.
    /// Results are ordered by timestamp descending (newest first).
    /// </summary>
    /// <param name="filter">Filter and pagination parameters.</param>
    /// <returns>Paginated list of audit log entries.</returns>
    /// <response code="200">Returns the filtered audit log list.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] AuditLogFilterRequest filter)
    {
        var query = _db.AuditLogs
            .Include(a => a.ActorUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(a => a.EntityId == filter.EntityId);

        var actionTypes = BuildStringList(filter.ActionTypes, filter.ActionType);
        if (actionTypes.Count > 0)
            query = query.Where(a => actionTypes.Contains(a.ActionType));

        var actorUserIds = BuildGuidList(filter.ActorUserIds, filter.ActorUserId);
        if (actorUserIds.Count > 0)
            query = query.Where(a => a.ActorUserId.HasValue && actorUserIds.Contains(a.ActorUserId.Value));

        if (!string.IsNullOrWhiteSpace(filter.ActorRole) &&
            Enum.TryParse<UserRole>(filter.ActorRole, ignoreCase: true, out var role))
            query = query.Where(a => a.ActorUser != null && a.ActorUser.Role == role);

        if (filter.From.HasValue)
            query = query.Where(a => a.Timestamp >= filter.From.Value);

        if (filter.To.HasValue)
        {
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
    /// Returns all audit log entries for a specific entity (e.g. all changes made to a particular reservation).
    /// Results are ordered by timestamp descending.
    /// </summary>
    /// <param name="entityType">Entity type name, e.g. <c>Reservation</c>, <c>Room</c>, <c>User</c>.</param>
    /// <param name="entityId">String representation of the entity Id (GUID or integer).</param>
    /// <returns>List of audit log entries for the given entity.</returns>
    /// <response code="200">Returns the entity audit history.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpGet("{entityType}/{entityId}")]
    [ProducesResponseType(typeof(List<AuditLogResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// Returns a distinct list of all action type strings present in the audit log.
    /// Intended for populating filter dropdowns on the frontend.
    /// Example values: <c>Create</c>, <c>Update</c>, <c>Delete</c>, <c>Cancel</c>, <c>PaymentConfirmed</c>.
    /// </summary>
    /// <returns>Sorted list of unique action type strings.</returns>
    /// <response code="200">Returns the list of action types.</response>
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
    /// Merges a list and a single value into one deduplicated list.
    /// </summary>
    private static List<string> BuildStringList(List<string>? list, string? single)
    {
        var result = new List<string>();
        if (list?.Count > 0) result.AddRange(list.Where(x => !string.IsNullOrWhiteSpace(x)));
        if (!string.IsNullOrWhiteSpace(single) && !result.Contains(single)) result.Add(single);
        return result;
    }

    /// <summary>
    /// Merges a list of GUIDs and a single GUID into one deduplicated list.
    /// </summary>
    private static List<Guid> BuildGuidList(List<Guid>? list, Guid? single)
    {
        var result = new List<Guid>();
        if (list?.Count > 0) result.AddRange(list);
        if (single.HasValue && !result.Contains(single.Value)) result.Add(single.Value);
        return result;
    }
}