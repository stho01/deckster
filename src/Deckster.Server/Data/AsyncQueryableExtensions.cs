using System.Linq.Expressions;
using Marten.Linq;

namespace Deckster.Server.Data;

public static class AsyncQueryableExtensions
{
    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        if (queryable is IMartenQueryable<T> marten)
        {
            return Marten.QueryableExtensions.FirstOrDefaultAsync(marten, cancellationToken);
        }
        
        return Task.FromResult(queryable.FirstOrDefault());
    }
    
    public static Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        if (queryable is IMartenQueryable<T> marten)
        {
            return Marten.QueryableExtensions.FirstOrDefaultAsync(marten, predicate, cancellationToken);
        }
        return Task.FromResult(queryable.FirstOrDefault(predicate));
    }

    public static Task<IReadOnlyList<T>> ToListAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        if (queryable is IMartenQueryable<T> marten)
        {
            return Marten.QueryableExtensions.ToListAsync(marten, cancellationToken);
        }
        return Task.FromResult<IReadOnlyList<T>>(queryable.ToList().AsReadOnly());
    }
}