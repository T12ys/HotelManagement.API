using HotelWebApplication.Data;
using HotelWebApplication.DTOs.AuthDTOs;
using HotelWebApplication.Models;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace HotelWebApplication.Services;

//Примечание: AuthService не использует IHttpContextAccessor — контроллер будет управлять cookie.

public class AuthService : IAuthService
{
    private readonly HotelDbContext _db;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AuthService> _logger;

    public AuthService(HotelDbContext db, IConfiguration cfg, ILogger<AuthService> logger)
    {
        _db = db;
        _cfg = cfg;
        _logger = logger;
    }

    // ---------------- LOGIN ----------------
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email, ct);
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var access = GenerateAccessToken(user, out var expiresAt);

        // генерируем refresh токен
        var refresh = CreateRefreshTokenString();
        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken(refresh), // храним только хэш
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_cfg["Jwt:RefreshTokenDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshEntity);
        await _db.SaveChangesAsync(ct);

        Console.WriteLine($"Login attempt: {dto.Email} / {dto.Password}");

        return new AuthResponseDto
        {
            AccessToken = access,
            AccessTokenExpiresAt = expiresAt,
            RefreshToken = refresh, // клиент получает оригинал
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        };
    }

    // ---------------- REFRESH ----------------
    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedAccessException("Refresh token missing.");

        var hash = HashToken(refreshToken);

        var existing = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hash, ct);

        if (existing == null || existing.RevokedAt != null || existing.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var user = existing.User;
        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("User not found or inactive.");

        // Ревокируем текущий токен
        existing.RevokedAt = DateTime.UtcNow;

        // Генерируем новый токен
        var newRefresh = CreateRefreshTokenString();
        var newRefreshEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken(newRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_cfg["Jwt:RefreshTokenDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(newRefreshEntity);
        await _db.SaveChangesAsync(ct);

        var access = GenerateAccessToken(user, out var expiresAt);

        return new AuthResponseDto
        {
            AccessToken = access,
            AccessTokenExpiresAt = expiresAt,
            RefreshToken = newRefresh, // клиент получает новый оригинал
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        };
    }

    // ---------------- LOGOUT ----------------
    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(refreshToken)) return;

        var hash = HashToken(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == hash, ct);
        if (existing == null) return;

        existing.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ---------------- REGISTER ----------------
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email, ct);
        if (exists) throw new InvalidOperationException("User with this email already exists.");

        var salt = Guid.NewGuid().ToString();
        var pwdHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            PasswordHash = pwdHash,
            Salt = salt,
            SecurityStamp = Guid.NewGuid().ToString(),
            Role = HotelWebApplication.Enums.UserRole.Customer,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Генерируем токены
        var access = GenerateAccessToken(user, out var expiresAt);
        var refresh = CreateRefreshTokenString();

        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = HashToken(refresh),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_cfg["Jwt:RefreshTokenDays"] ?? "7")),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(refreshEntity);
        await _db.SaveChangesAsync(ct);

        return new AuthResponseDto
        {
            AccessToken = access,
            AccessTokenExpiresAt = expiresAt,
            RefreshToken = refresh,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        };
    }

    // ---------------- HELPERS ----------------
    private string GenerateAccessToken(User user, out DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var minutes = int.Parse(_cfg["Jwt:AccessTokenMinutes"] ?? "15");
        expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var token = new JwtSecurityToken(
            issuer: _cfg["Jwt:Issuer"],
            audience: _cfg["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        return tokenHandler.WriteToken(token);
    }

    private static string CreateRefreshTokenString()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}