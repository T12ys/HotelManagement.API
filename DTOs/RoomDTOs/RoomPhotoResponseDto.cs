namespace HotelWebApplication.DTOs.RoomDTOs;

public class RoomPhotoResponseDto
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public int SortOrder { get; set; }
    public string? AltText { get; set; }
}
