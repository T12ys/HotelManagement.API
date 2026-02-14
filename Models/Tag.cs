namespace HotelWebApplication.Models
{
    public class Tag
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Slug { get; set; } = null!;

        public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
    }
}
