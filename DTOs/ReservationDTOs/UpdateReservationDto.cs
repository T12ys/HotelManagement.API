using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.ReservationDTOs;

public class UpdateReservationDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public ReservationStatus? Status { get; set; }

    public string? Notes { get; set; }
}