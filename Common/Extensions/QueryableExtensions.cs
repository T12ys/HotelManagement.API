using System.Linq.Expressions;

namespace HotelWebApplication.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        var sorts = sortBy.Split(',');

        bool first = true;

        foreach (var sort in sorts)
        {
            var parts = sort.Split(':');
            var propertyName = parts[0];
            var direction = parts.Length > 1 ? parts[1] : "asc";

            query = ApplyOrder(query, propertyName, direction, first);

            first = false;
        }

        return query;
    }

    private static IQueryable<T> ApplyOrder<T>(
        IQueryable<T> source,
        string propertyName,
        string direction,
        bool first)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var property = Expression.PropertyOrField(parameter, propertyName);

        var lambda = Expression.Lambda(property, parameter);

        string methodName;

        if (first)
        {
            methodName = direction == "desc"
                ? "OrderByDescending"
                : "OrderBy";
        }
        else
        {
            methodName = direction == "desc"
                ? "ThenByDescending"
                : "ThenBy";
        }

        var result = Expression.Call(
            typeof(Queryable),
            methodName,
            new Type[] { typeof(T), property.Type },
            source.Expression,
            Expression.Quote(lambda));

        return source.Provider.CreateQuery<T>(result);
    }
}