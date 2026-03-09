using HotelWebApplication.Enums;

namespace HotelWebApplication.Models;

public class PriceRule
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public RuleType RuleType { get; set; }
    public int? RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsIncrease { get; set; }   
    public bool IsPercent { get; set; }                    
    public decimal Value { get; set; }                    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}