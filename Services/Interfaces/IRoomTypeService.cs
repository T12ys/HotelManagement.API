using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface IRoomTypeService
{
    // READ (для всех)

    Task<PagedResult<RoomTypeResponseDto>> GetPagedAsync(RoomTypeFilterRequest request, CancellationToken ct = default);

    Task<RoomTypeResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

    // ADMIN ONLY

    Task<int> CreateAsync(CreateRoomTypeDto dto, IEnumerable<IFormFile>? photos, CancellationToken ct = default);

    Task UpdateAsync(int id, UpdateRoomTypeDto dto, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    // MODERATOR (Photos only)

    Task AddPhotosAsync(int roomTypeId, IEnumerable<IFormFile> photos, CancellationToken ct = default);

    Task DeletePhotoAsync(int photoId, CancellationToken ct = default);
}
