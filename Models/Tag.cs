namespace HotelWebApplication.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Slug { get; set; } = null!;
        public Dictionary<string, string> Translations { get; set; } = new();
        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    }
}