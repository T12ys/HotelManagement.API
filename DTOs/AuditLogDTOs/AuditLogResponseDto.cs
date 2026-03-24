namespace HotelWebApplication.DTOs.AuditLogDTOs;

public class AuditLogResponseDto
{
    public long Id { get; set; }
    public string ActionType { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string? ActorRole { get; set; }   // "Admin", "Moderator", "Customer", null (system)
    public string? IP { get; set; }
    public DateTime Timestamp { get; set; }
}