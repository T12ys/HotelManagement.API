using HotelWebApplication.Enums;

namespace HotelWebApplication.Models
{
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // 🔥 Теперь бронь привязана к Room
        public int RoomId { get; set; }
        public Room Room { get; set; } = null!;

        public string CustomerName { get; set; } = null!;
        public string CustomerEmail { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalPrice { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }
        public DateTime? HeldUntil { get; set; }

        public string? Notes { get; set; }

        public string Source { get; set; } = "web";

        // оптимистичная блокировка
        public byte[]? ConcurrencyToken { get; set; }

        public ICollection<ReservationItem> ReservationItems { get; set; }
            = new List<ReservationItem>();
    }
}
