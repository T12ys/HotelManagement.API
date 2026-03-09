namespace HotelWebApplication.DTOs.PriceDTOs;

public class PeriodRulesRequestDto
{
    public int RoomTypeId { get; set; }

    public DateTime From { get; set; }

    // Конец периода — если фронт не передал, бек подставит год вперёд ( в контролере)
    public DateTime To { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}