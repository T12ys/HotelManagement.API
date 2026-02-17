using HotelWebApplication.DTOs.AuthDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
}
