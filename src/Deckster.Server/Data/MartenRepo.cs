using Marten;

namespace Deckster.Server.Data;

public class MartenRepo : IRepo, IDisposable, IAsyncDisposable
{
    private readonly IDocumentSession _session;

    public MartenRepo(IDocumentStore store)
    {
        _session = store.LightweightSession();
    }
    
    public Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : DatabaseObject
    {
        return _session.LoadAsync<T>(id, cancellationToken);
    }

    public async Task SaveAsync<T>(T item, CancellationToken cancellationToken = default) where T : DatabaseObject
    {
        
        _session.Store(item);
        await _session.SaveChangesAsync(cancellationToken);
    }

    public IQueryable<T> Query<T>() where T : DatabaseObject
    {
        return _session.Query<T>();
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _session.DisposeAsync();
    }
}