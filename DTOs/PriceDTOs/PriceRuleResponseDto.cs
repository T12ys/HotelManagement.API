using HotelWebApplication.Enums;

namespace HotelWebApplication.DTOs.PriceDTOs;

public class PriceRuleResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public RuleType RuleType { get; set; }
    public int? RoomTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsIncrease { get; set; }
    public bool IsPercent { get; set; }
    public decimal Value { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

}