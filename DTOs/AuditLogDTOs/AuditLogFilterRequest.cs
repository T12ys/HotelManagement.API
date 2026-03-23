using HotelWebApplication.Common.Pagination;

namespace HotelWebApplication.DTOs.AuditLogDTOs;

public class AuditLogFilterRequest : PagedRequest
{
    /// <summary>
    /// Фильтр по типу сущности: "Reservation", "Room", "RoomType", "PriceRule", "Tag", "User"
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Фильтр по конкретному Id сущности
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Фильтр по одному типу действия (для простых запросов)
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Фильтр по нескольким типам действий (мультивыбор).
    /// Пример: ?ActionTypes=Create&amp;ActionTypes=Update&amp;ActionTypes=Delete
    /// </summary>
    public List<string>? ActionTypes { get; set; }

    /// <summary>
    /// Фильтр по одному пользователю (для простых запросов)
    /// </summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// Фильтр по нескольким пользователям (мультивыбор).
    /// Пример: ?ActorUserIds=guid1&amp;ActorUserIds=guid2
    /// </summary>
    public List<Guid>? ActorUserIds { get; set; }

    /// <summary>
    /// Фильтр по роли пользователя: "Admin", "Moderator", "Customer"
    /// </summary>
    public string? ActorRole { get; set; }

    /// <summary>
    /// Начало диапазона дат
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// Конец диапазона дат
    /// </summary>
    public DateTime? To { get; set; }
}