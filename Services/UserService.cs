using HotelWebApplication.Common.Extensions;
using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Data;
using HotelWebApplication.DTOs.UserDTOs;
using HotelWebApplication.Enums;
using HotelWebApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HotelWebApplication.Services;

public class UserService : IUserService
{
    private readonly HotelDbContext _db;
    private readonly IAuditLogService _audit;

    public UserService(HotelDbContext db, IAuditLogService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<UserResponseDto> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct)
            ?? throw new KeyNotFoundException("User not found.");

        return MapToDto(user);
    }

    public async Task<UserResponseDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var oldSnapshot = JsonSerializer.Serialize(new
        {
            user.Email,
            user.DisplayName,
            user.PhoneNumber
        });

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId, ct);
            if (emailTaken)
                throw new InvalidOperationException("This email is already taken.");

            user.Email = dto.Email;
        }

        if (!string.IsNullOrWhiteSpace(dto.DisplayName))
            user.DisplayName = dto.DisplayName;

        if (dto.PhoneNumber != null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "UpdateProfile",
            entityType: "User",
            entityId: userId.ToString(),
            oldValue: oldSnapshot,
            newValue: JsonSerializer.Serialize(new
            {
                user.Email,
                user.DisplayName,
                user.PhoneNumber
            }),
            actorUserId: actorUserId ?? userId,
            ip: ip);

        return MapToDto(user);
    }

    // Перегрузка для обратной совместимости (вызов из контроллера без actor)
    public Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default)
        => UpdateProfileAsync(userId, dto, ct, null, null);

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString();

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "ChangePassword",
            entityType: "User",
            entityId: userId.ToString(),
            actorUserId: actorUserId ?? userId,
            ip: ip);
    }

    public Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default)
        => ChangePasswordAsync(userId, dto, ct, null, null);

    public async Task<PagedResult<UserResponseDto>> GetAllUsersAsync(PagedRequest request, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.DisplayName.ToLower().Contains(s));
        }

        query = query.ApplySorting(request.SortBy ?? "CreatedAt:asc");

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserResponseDto>(
            items.Select(MapToDto), total, request.Page, request.PageSize);
    }

    public async Task<UserResponseDto> UpdateUserRoleAsync(
        Guid userId,
        UpdateUserRoleDto dto,
        CancellationToken ct = default,
        Guid? actorUserId = null,
        string? ip = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (dto.Role == UserRole.Admin)
            throw new InvalidOperationException("Cannot assign Admin role.");

        var oldRole = user.Role;
        user.Role = dto.Role;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            actionType: "UpdateRole",
            entityType: "User",
            entityId: userId.ToString(),
            oldValue: JsonSerializer.Serialize(new { Role = oldRole.ToString() }),
            newValue: JsonSerializer.Serialize(new { Role = dto.Role.ToString() }),
            actorUserId: actorUserId,
            ip: ip);

        return MapToDto(user);
    }

    public Task<UserResponseDto> UpdateUserRoleAsync(Guid userId, UpdateUserRoleDto dto, CancellationToken ct = default)
        => UpdateUserRoleAsync(userId, dto, ct, null, null);

    private static UserResponseDto MapToDto(Models.User user) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        PhoneNumber = user.PhoneNumber,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}