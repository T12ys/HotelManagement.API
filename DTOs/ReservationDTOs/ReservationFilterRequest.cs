using HotelWebApplication.Common.Pagination;
using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.ReservationDTOs;

public class ReservationFilterRequest : PagedRequest
{
    public int? RoomTypeId { get; set; }
    public int? RoomId { get; set; }
    public ReservationStatus? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? CustomerEmail { get; set; }
}