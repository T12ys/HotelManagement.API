using HotelWebApplication.DTOs.AuthDTOs;
using HotelWebApplication.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelWebApplication.Controllers;

/// <summary>
/// Handles user authentication: login, registration, token refresh and logout.
/// Refresh tokens are stored in an HttpOnly cookie for security.
/// Access tokens are returned in the response body.
/// </summary>
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

    /// <summary>
    /// Authenticates a user with email and password.
    /// Returns a JWT access token and sets an HttpOnly refresh token cookie.
    /// </summary>
    /// <param name="dto">User credentials (email + password).</param>
    /// <returns>Access token, expiry time, user info and role.</returns>
    /// <response code="200">Login successful. Returns access token and user data.</response>
    /// <response code="401">Invalid email or password.</response>
    /// <response code="422">Validation failed (empty fields, invalid email format).</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        if (!string.IsNullOrEmpty(result.RefreshToken))
            SetRefreshTokenCookie(result.RefreshToken, result.AccessTokenExpiresAt);
        return Ok(result);
    }

    /// <summary>
    /// Issues a new access token using a valid refresh token.
    /// The refresh token is read from the HttpOnly cookie first; falls back to the request body.
    /// Old refresh token is rotated (revoked and replaced).
    /// </summary>
    /// <param name="dto">Optional body with refresh token (used if cookie is absent).</param>
    /// <returns>New access token and updated user data.</returns>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Refresh token is missing, expired or already revoked.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto? dto)
    {
        var cookieToken = Request.Cookies["refreshToken"];
        var token = cookieToken ?? dto?.RefreshToken;
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { message = "Refresh token missing." });

        var result = await _auth.RefreshTokenAsync(token);
        if (!string.IsNullOrEmpty(result.RefreshToken))
        {
            SetRefreshTokenCookie(result.RefreshToken, result.AccessTokenExpiresAt);
            result.RefreshToken = null;
        }
        return Ok(result);
    }

    /// <summary>
    /// Revokes the current refresh token and clears the refresh token cookie.
    /// Requires a valid access token (Bearer).
    /// </summary>
    /// <param name="dto">Optional body with refresh token (used if cookie is absent).</param>
    /// <response code="204">Logout successful.</response>
    /// <response code="401">Access token is missing or invalid.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto? dto)
    {
        var cookieToken = Request.Cookies["refreshToken"];
        var token = cookieToken ?? dto?.RefreshToken;
        if (!string.IsNullOrEmpty(token))
        {
            await _auth.LogoutAsync(token);
            Response.Cookies.Delete("refreshToken");
        }
        return NoContent();
    }

    /// <summary>
    /// Registers a new customer account.
    /// Returns a JWT access token and sets a refresh token cookie — the user is logged in immediately.
    /// </summary>
    /// <param name="dto">Registration data: email, display name, phone (optional) and password.</param>
    /// <returns>Access token and newly created user data.</returns>
    /// <response code="200">Registration successful. Returns access token and user data.</response>
    /// <response code="409">An account with this email already exists.</response>
    /// <response code="422">Validation failed (weak password, invalid email, etc.).</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
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

    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes the refresh token into an HttpOnly cookie.
    /// Uses Secure flag in production and Lax SameSite for cross-origin requests.
    /// </summary>
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