namespace HotelWebApplication.DTOs.RoomDTOs;

public class CreateRoomTypeDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    // Список тегов по id
    public List<int>? TagIds { get; set; }
}
