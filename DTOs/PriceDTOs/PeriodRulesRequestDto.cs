namespace HotelWebApplication.DTOs.PriceDTOs;

public class PeriodRulesRequestDto
{
    public int RoomTypeId { get; set; }

    public DateTime From { get; set; }

    // End of period - if the front has not transmitted, the back will substitute the year ahead (in the controller)
    public DateTime To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}