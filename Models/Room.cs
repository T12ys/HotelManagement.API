namespace HotelWebApplication.Models;

public class Room
{
    public int Id { get; set; }

    // Номер комнаты или внутренний код
    public string Number { get; set; } = null!;

    public int RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = null!;

    public int Floor { get; set; }

    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
