namespace HotelWebApplication.DTOs.PriceDTOs;

public class PriceCalculationResponseDto
{
    public int RoomTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Nights { get; set; }
    public decimal BaseTotalPrice { get; set; }
    public decimal FinalTotalPrice { get; set; }
    public List<DailyPriceDto> DailyBreakdown { get; set; } = new();
}

public class DailyPriceDto
{
    public DateTime Date { get; set; }

    // Base price from RoomType (before all rules (modifiers) are taken into account)
    public decimal BasePrice { get; set; }

    // The final price for 1 day after taking into account all the rules for that day
    public decimal FinalPrice { get; set; }

    public bool HasModifiers => AppliedRules.Count > 0;

    // List of applied rules
    public List<AppliedRuleDto> AppliedRules { get; set; } = new();
}

public class AppliedRuleDto
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = null!;

    // Price change in absolute numbers for this day
    // Negative = discount, positive = premium
    public decimal PriceDelta { get; set; }
}