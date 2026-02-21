using HotelWebApplication.DTOs.AuthDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;

namespace HotelWebApplication.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;

    public AuthController(IAuthService auth, IConfiguration cfg, IWebHostEnvironment env)
    {
        _auth = auth;
        _cfg = cfg;
        _env = env;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        // Set refresh token as httpOnly cookie
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            SetRefreshTokenCookie(result.RefreshToken, result.AccessTokenExpiresAt);
            //result.RefreshToken = null;
        }
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto? dto)
    {
        // Prefer cookie
        var cookieToken = Request.Cookies["refreshToken"];
        var token = cookieToken ?? dto?.RefreshToken;
        if (string.IsNullOrEmpty(token)) return Unauthorized(new { message = "Refresh token missing" });

        var result = await _auth.RefreshTokenAsync(token);

        // rotate cookie
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            SetRefreshTokenCookie(result.RefreshToken, result.AccessTokenExpiresAt);
            result.RefreshToken = null;
        }
        return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? dto)
    {
        var cookieToken = Request.Cookies["refreshToken"];
        var token = cookieToken ?? dto?.RefreshToken;
        if (!string.IsNullOrEmpty(token))
        {
            await _auth.LogoutAsync(token);
            // remove cookie
            Response.Cookies.Delete("refreshToken");
        }
        return NoContent();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _auth.RegisterAsync(dto);
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            SetRefreshTokenCookie(result.RefreshToken, result.AccessTokenExpiresAt);
            result.RefreshToken = null;
        }
        return Ok(result);
    }

    //🔹 локально → HTTP cookie
    //🔹 на сервере → HTTPS cookie
    private void SetRefreshTokenCookie(string refreshToken, DateTime accessExpiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _env.IsProduction(),
            Expires = DateTime.UtcNow.AddDays(
        int.Parse(_cfg["Jwt:RefreshTokenDays"] ?? "7")),
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
}
