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

    // Базовая цена из RoomType (до  учета всех правил (модификаторов))
    public decimal BasePrice { get; set; }

    // Итоговая цена на 1 день после после учета всех правил на этот день
    public decimal FinalPrice { get; set; }

    public bool HasModifiers => AppliedRules.Count > 0;

    // Список применённых правил
    public List<AppliedRuleDto> AppliedRules { get; set; } = new();
}

public class AppliedRuleDto
{
    public int RuleId { get; set; }
    public string RuleName { get; set; } = null!;

    // Изменение цены в абсолютных числах на этот день
    // Отрицательное = скидка, положительное = надбавка
    public decimal PriceDelta { get; set; }
}