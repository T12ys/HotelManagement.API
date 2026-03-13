using HotelWebApplication.Common.Pagination;

namespace HotelWebApplication.DTOs.AuditLogDTOs;

public class AuditLogFilterRequest : PagedRequest
{
    public string? EntityType { get; set; }   // "Reservation", "Room" и т.д.
    public string? EntityId { get; set; }     // конкретная запись
    public string? ActionType { get; set; }   // "Create", "Cancel" и т.д.
    public Guid? ActorUserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}