using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.ReservationDTOs;

public class ReservationResponseDto
{
    public Guid Id { get; set; }

    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = null!;   // Room.Number для удобства UI

    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int NightsCount { get; set; }

    public decimal TotalPrice { get; set; }

    public ReservationStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// До какого момента бронь удерживается (hold 15 минут)
    /// </summary>
    public DateTime? HeldUntil { get; set; }

    public string? Notes { get; set; }

    public string Source { get; set; } = "web";

    public List<ReservationItemResponseDto> Items { get; set; } = new();
}