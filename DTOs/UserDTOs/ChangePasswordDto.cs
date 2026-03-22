namespace HotelWebApplication.DTOs.UserDTOs;

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}