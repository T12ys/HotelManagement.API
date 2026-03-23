using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.UserDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<UserResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task<PagedResult<UserResponseDto>> GetAllUsersAsync(PagedRequest request, CancellationToken ct = default);
    Task<UserResponseDto> UpdateUserRoleAsync(Guid userId, UpdateUserRoleDto dto, CancellationToken ct = default);
}