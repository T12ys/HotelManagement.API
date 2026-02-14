namespace HotelWebApplication.Models
{
    public class PriceRule
    {
        public int Id { get; set; }

        // null = глобальное правило (для всех типов номеров)
        public int? RoomTypeId { get; set; }

        public RoomType? RoomType { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        // Если IsPercentage = false → это фиксированная цена
        // Если true → это % надбавка/скидка
        public decimal Price { get; set; }

        public bool IsPercentage { get; set; }

        // Чем выше Priority — тем важнее правило
        public int Priority { get; set; }
    }
}
