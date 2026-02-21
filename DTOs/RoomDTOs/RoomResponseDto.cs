namespace HotelWebApplication.DTOs.RoomDTOs;

public class RoomResponseDto
{
    public int Id { get; set; }
    public string Number { get; set; } = null!;
    public int Floor { get; set; }
    public bool IsAvailable { get; set; }
    public int RoomTypeId { get; set; }
}
