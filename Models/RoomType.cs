using Azure;

namespace HotelWebApplication.Models;

public class RoomType
{
    public int Id { get; set; }

    public string Code { get; set; } = null!; // enum-friendly (e.g. STANDARD, DELUXE) В дальнейшем проверить лучше сделать просто string или все же перейти на enum 

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Capacity { get; set; }

    public int MaxOccupancyAdults { get; set; }

    public int MaxOccupancyChildren { get; set; }

    public decimal BasePrice { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public ICollection<RoomPhoto> Photos { get; set; } = new List<RoomPhoto>();

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
