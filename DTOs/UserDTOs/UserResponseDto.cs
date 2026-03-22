using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.UserDTOs;

public class UserResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
}