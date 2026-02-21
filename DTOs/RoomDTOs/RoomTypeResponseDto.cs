namespace HotelWebApplication.DTOs.RoomDTOs;

public class RoomTypeResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Capacity { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; }
    public IEnumerable<RoomPhotoResponseDto> Photos { get; set; } = Array.Empty<RoomPhotoResponseDto>();
    public IEnumerable<TagResponseDto> Tags { get; set; } = Array.Empty<TagResponseDto>();
}
