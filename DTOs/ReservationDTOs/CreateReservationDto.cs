namespace HotelWebApplication.DTOs.ReservationDTOs;

public class CreateReservationDto
{
    public int RoomTypeId { get; set; }

    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int GuestCount { get; set; }

    public string? Notes { get; set; }

    // Дополнительные услуги (опционально)
    public List<ReservationItemDto>? Items { get; set; }
}