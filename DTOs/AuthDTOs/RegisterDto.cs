namespace HotelWebApplication.DTOs.AuthDTOs;

public class RegisterDto
{
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Password { get; set; } = null!;
}
