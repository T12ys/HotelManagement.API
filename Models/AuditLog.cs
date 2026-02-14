namespace HotelWebApplication.Models
{
    public class AuditLog
    {
        public long Id { get; set; }

        public Guid? ActorUserId { get; set; }

        public User? ActorUser { get; set; }

        public string ActionType { get; set; } = null!; // Create, Update, Delete

        public string EntityType { get; set; } = null!;

        public string EntityId { get; set; } = null!;

        public string? OldValue { get; set; } // JSON

        public string? NewValue { get; set; } // JSON

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string? IP { get; set; }
    }
}
