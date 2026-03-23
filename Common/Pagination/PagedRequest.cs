namespace HotelWebApplication.Common.Pagination;

public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Sorting type: "name:asc,basePrice:desc"
    public string? SortBy { get; set; }

    // Custom filter string (the service will parse/apply)
    public string? Search { get; set; }
}
