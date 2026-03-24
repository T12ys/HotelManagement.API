using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.PriceDTOs;

public class CreatePriceRuleDto
{
    public string Name { get; set; } = null!;
    public RuleType RuleType { get; set; }

    // null = rule for all types of numbers
    public int? RoomTypeId { get; set; }

    public DateTime StartDate { get; set; }

    // For SpecialDate it must match StartDate - the validator will check
    public DateTime EndDate { get; set; }

    public bool IsIncrease { get; set; }    // true = surcharge, false = discount
    public bool IsPercent { get; set; }     // true = %, false = absolute number
    public decimal Value { get; set; }      // always positive
}