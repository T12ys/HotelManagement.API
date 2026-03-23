namespace HotelWebApplication.DTOs.RoomDTOs;

public class TagResponseDto
{
    public int Id { get; set; }
    public string Slug { get; set; } = null!;
    public Dictionary<string, string> Translations { get; set; } = new();
}