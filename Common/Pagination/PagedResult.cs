namespace HotelWebApplication.Common.Pagination;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public PagedResult() { }

    public PagedResult(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        Items = items.ToList().AsReadOnly();
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}