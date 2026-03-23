using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.ReservationDTOs;

public class ReservationResponseDto
{
    public Guid Id { get; set; }

    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = null!;

    // Room type - needed for grouping in the calendar
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = null!;

    public Guid? UserId { get; set; }
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
    public DateTime? HeldUntil { get; set; }

    public string? Notes { get; set; }

    public string Source { get; set; } = "web";

    public List<ReservationItemResponseDto> Items { get; set; } = new();
}