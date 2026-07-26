using System.Linq.Expressions;

namespace CloneAmazonBack.Extensions;

public static class QueryExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? query.Where(predicate) : query;
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, Guid? value, Expression<Func<T, bool>> predicate)
    {
        return value.HasValue ? query.Where(predicate) : query;
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool? value, Expression<Func<T, bool>> predicate)
    {
        return value.HasValue ? query.Where(predicate) : query;
    }
}
