namespace HotelWebApplication.Common.Pagination;

public class RoomTypeFilterRequest : PagedRequest
{
    public string? Code { get; set; }
    public bool? IsActive { get; set; }

    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }

    public int? MinAdults { get; set; }
    public int? MaxAdults { get; set; }

    public int? MinChildren { get; set; }
    public int? MaxChildren { get; set; }

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    public List<int>? TagIds { get; set; }
}
