using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.AuthDTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAt { get; set; }
    // We will not return refresh token in body in recommended flow.
    public string? RefreshToken { get; set; } // optional for non-cookie flows
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public UserRole Role { get; set; }
}