using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.PriceDTOs;

public class UpdatePriceRuleDto
{
    // RoomTypeId is intentionally missing - we don't change the room type after creation

    public string Name { get; set; } = null!;
    public RuleType RuleType { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool IsIncrease { get; set; }
    public bool IsPercent { get; set; }
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
}