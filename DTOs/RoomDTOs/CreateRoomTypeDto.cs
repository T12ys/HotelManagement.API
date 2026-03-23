namespace HotelWebApplication.DTOs.RoomDTOs;

public class CreateRoomTypeDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Capacity { get; set; }
    public int MaxOccupancyAdults { get; set; }
    public int MaxOccupancyChildren { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int>? TagIds { get; set; }
}
