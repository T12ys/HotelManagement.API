namespace HotelWebApplication.Models
{
    public class ReservationItem
    {
        public int Id { get; set; }

        public Guid ReservationId { get; set; }

        public Reservation Reservation { get; set; } = null!;

        public string Name { get; set; } = null!; // Завтрак, Трансфер

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal Total => Price * Quantity;
    }
}
