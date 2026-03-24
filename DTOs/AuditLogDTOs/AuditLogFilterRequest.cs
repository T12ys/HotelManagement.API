using HotelWebApplication.Common.Pagination;

namespace HotelWebApplication.DTOs.AuditLogDTOs;

public class AuditLogFilterRequest : PagedRequest
{
    /// <summary>
    /// Filter by entity type: "Reservation", "Room", "RoomType", "PriceRule", "Tag", "User"
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Filter by a specific entity ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Filter by one action type (for simple queries)
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Filter by multiple action types (multi-select).
    /// Example: ?ActionTypes=Create&amp;ActionTypes=Update&amp;ActionTypes=Delete
    /// </summary>
    public List<string>? ActionTypes { get; set; }

    /// <summary>
    /// Filter by one user (for simple queries)
    /// </summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// Filter by multiple users (multi-select).
    /// Example: ?ActorUserIds=guid1&amp;ActorUserIds=guid2
    /// </summary>
    public List<Guid>? ActorUserIds { get; set; }

    /// <summary>
    /// Filter by user role: "Admin", "Moderator", "Customer"
    /// </summary>
    public string? ActorRole { get; set; }

    /// <summary>
    /// Start of date range
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// End of date range
    /// </summary>
    public DateTime? To { get; set; }
}