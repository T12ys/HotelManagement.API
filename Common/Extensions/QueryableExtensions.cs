using System.Linq.Expressions;

namespace HotelWebApplication.Common.Extensions;

public static class QueryableExtensions
{
    /// <summary>
    /// Applies dynamic sorting to a query based on a comma-separated sort string.
    /// Format: <c>"propertyName:asc,otherProperty:desc"</c>.
    /// If <paramref name="sortBy"/> is null or empty, the query is returned unchanged.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="sortBy">
    /// Sort expression string, e.g. <c>"basePrice:asc,name:desc"</c>.
    /// Direction defaults to <c>asc</c> if omitted.
    /// </param>
    /// <returns>The queryable with ordering applied.</returns>
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

    /// <summary>
    /// Builds and applies a single ORDER BY / THEN BY expression to the query using reflection.
    /// Uses <c>OrderBy</c> / <c>OrderByDescending</c> for the first sort clause
    /// and <c>ThenBy</c> / <c>ThenByDescending</c> for subsequent clauses.
    /// </summary>
    /// <typeparam name="T">The entity type being queried.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="propertyName">Name of the property to sort by.</param>
    /// <param name="direction"><c>"asc"</c> for ascending, <c>"desc"</c> for descending.</param>
    /// <param name="first"><c>true</c> if this is the first sort clause (uses OrderBy); <c>false</c> for ThenBy.</param>
    /// <returns>The queryable with the ordering clause appended.</returns>
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