using HotelWebApplication.Common.Pagination;
using HotelWebApplication.DTOs.ReservationDTOs;

namespace HotelWebApplication.Services.Interfaces;

public interface IReservationService
{
    /// <summary>
    /// Создать бронь: атомарная проверка доступности + Pending + HeldUntil
    /// </summary>
    Task<ReservationResponseDto> CreateAsync(CreateReservationDto dto, string? ip = null);

    /// <summary>
    /// Получить бронь по Id
    /// </summary>
    Task<ReservationResponseDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Список броней (для admin/moderator) с фильтрацией
    /// </summary>
    Task<PagedResult<ReservationResponseDto>> GetAllAsync(ReservationFilterRequest filter);

    /// <summary>
    /// Обновить даты / статус / заметки (admin/moderator)
    /// </summary>
    Task<ReservationResponseDto> UpdateAsync(Guid id, UpdateReservationDto dto, Guid actorUserId, string? ip = null);

    /// <summary>
    /// Отменить бронь (admin/moderator)
    /// </summary>
    Task CancelAsync(Guid id, Guid actorUserId, string? ip = null);

    /// <summary>
    /// Mock-оплата: Pending → Confirmed
    /// </summary>
    Task<ReservationResponseDto> ProcessMockPaymentAsync(Guid reservationId, bool simulateSuccess, string? ip = null);

    Task<PagedResult<ReservationResponseDto>> GetMyReservationsAsync(Guid userId, PagedRequest request);
}