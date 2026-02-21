using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.RoomDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface ITagService
{
    // READ

    Task<PagedResult<TagResponseDto>> GetPagedAsync(PagedRequest request, CancellationToken ct = default);

    Task<TagResponseDto?> GetByIdAsync(int id, CancellationToken ct = default);

    // WRITE (Admin + Moderator)

    Task<int> CreateAsync(CreateTagDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, CreateTagDto dto, CancellationToken ct = default);

    // DELETE (Admin only)

    Task DeleteAsync(int id, CancellationToken ct = default);
}
