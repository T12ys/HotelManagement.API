using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.ReservationDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface IReservationService
{
    /// <summary>
    /// Create a reservation: atomic availability check + Pending + HeldUntil
    /// </summary>
    Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, string? ip = null);

    /// <summary>
    /// Get a reservation by ID
    /// </summary>
    Task<ReservationResponseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// List of reservations (for admin/moderator) with filtering
    /// </summary>
    Task<PagedResult<ReservationResponseDto>> GetAllAsync(ReservationFilterRequest filter);

    /// <summary>
    /// Update dates / status / notes (admin/moderator)
    /// </summary>
    Task<ReservationResponseDto> UpdateAsync(Guid id, UpdateReservationDto dto, Guid actorUserId, string? ip = null);

    /// <summary>
    /// Cancel Reservation (admin/moderator)
    /// </summary>
    Task CancelAsync(Guid id, Guid actorUserId, string? ip = null);

    /// <summary>
    /// Mock payment: Pending → Confirmed
    /// </summary>
    Task<ReservationResponseDto> ProcessMockPaymentAsync(Guid reservationId, bool simulateSuccess, string? ip = null);

    Task<PagedResult<ReservationResponseDto>> GetMyReservationsAsync(Guid userId, PagedRequest request);
}