namespace HotelWebApplication.DTOs.RoomDTOs;

public class CreateRoomDto
{
    public string Number { get; set; } = null!;
    public int RoomTypeId { get; set; }
    public int Floor { get; set; }
}
