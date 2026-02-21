using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface IRoomService
{
    // READ (all roles)

    Task<PagedResult<RoomResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct = default);

    Task<RoomResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

    // WRITE (Admin + Moderator)

    Task<int> CreateAsync(CreateRoomDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, UpdateRoomDto dto, CancellationToken ct = default);

    // DELETE (Admin only)

    Task DeleteAsync(int id, CancellationToken ct = default);

    Task ChangeAvailabilityAsync(int id, ChangeRoomAvailabilityDto dto, CancellationToken ct = default);
}