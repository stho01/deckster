using System.Linq.Expressions;

namespace Deckster.Server.Data;

public static class AsyncQueryableExtensions
{
    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(queryable.FirstOrDefault());
    }
    
    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(queryable.FirstOrDefault(predicate));
    }

    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(queryable.ToList());
    }
}