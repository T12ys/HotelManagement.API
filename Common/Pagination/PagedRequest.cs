namespace HotelWebApplication.Common.Pagination;

public class PagedRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Сортировка вида: "name:asc,basePrice:desc"
    public string? SortBy { get; set; }

    // Произвольная строка фильтра (сервис будет парсить/применять)
    public string? Search { get; set; }
}
