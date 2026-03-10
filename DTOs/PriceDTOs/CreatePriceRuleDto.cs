using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.PriceDTOs;

public class CreatePriceRuleDto
{
    public string Name { get; set; } = null!;
    public RuleType RuleType { get; set; }

    // null = правило для всех типов номеров
    public int? RoomTypeId { get; set; }

    public DateTime StartDate { get; set; }

    // Для SpecialDate должен совпадать с StartDate — валидатор проверит
    public DateTime EndDate { get; set; }

    public bool IsIncrease { get; set; }    // true = надбавка, false = скидка
    public bool IsPercent { get; set; }     // true = %, false = абсолютное число
    public decimal Value { get; set; }      // всегда положительное
}