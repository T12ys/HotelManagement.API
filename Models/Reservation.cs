using HotelWebApplication.Enums;

namespace HotelWebApplication.Models
{
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int RoomTypeId { get; set; }

        public RoomType RoomType { get; set; } = null!;

        public string CustomerName { get; set; } = null!;

        public string CustomerEmail { get; set; } = null!;

        public string CustomerPhone { get; set; } = null!;

        public DateTime StartDate { get; set; }

        // EndDate exclusive
        public DateTime EndDate { get; set; }

        public decimal TotalPrice { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        // Если бронь "удерживается"
        public DateTime? HeldUntil { get; set; }

        public string? Notes { get; set; }

        // web / admin
        public string Source { get; set; } = "web";

        // Для оптимистичной блокировки
        public byte[]? ConcurrencyToken { get; set; }

        public ICollection<ReservationItem> ReservationItems { get; set; } = new List<ReservationItem>();

    }
}
