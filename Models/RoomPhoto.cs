namespace HotelWebApplication.Models
{
    public class RoomPhoto
    {
        public int Id { get; set; }

        public int RoomTypeId { get; set; }

        public RoomType RoomType { get; set; } = null!;

        public string Url { get; set; } = null!;

        public int SortOrder { get; set; }

        public string? AltText { get; set; }
    }
}
