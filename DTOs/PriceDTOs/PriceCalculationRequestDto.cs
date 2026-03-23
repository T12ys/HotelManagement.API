namespace HotelWebApplication.DTOs.PriceDTOs;

public class PriceCalculationRequestDto
{
    public int RoomTypeId { get; set; }

    public DateTime StartDate { get; set; }

    // Check-out date is not included in the price
    // Example: check-in on January 1, check-out on January 3 = 2 nights (January 1 and January 2)
    public DateTime EndDate { get; set; }
}