using Deckster.Games;
using Deckster.Games.Data;
using Deckster.Server.Games;
using Marten;

namespace Deckster.Server.Data;

public class MartenRepo : IRepo, IDisposable, IAsyncDisposable
{
    private readonly IDocumentStore _store;
    private readonly IDocumentSession _session;

    public MartenRepo(IDocumentStore store)
    {
        _store = store;
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

    public Task<T?> GetGameAsync<T>(Guid id, long version, CancellationToken cancellationToken = default) where T : GameObject
    {
        return _session.Events.AggregateStreamAsync<T>(id, version, token: cancellationToken);
    }

    public IEventQueue<T> StartEventStream<T>(Guid id, IEnumerable<object> startEvents) where T : GameObject
    {
        var session = _store.LightweightSession();
        return new MartenEventQueue<T>(id, session, startEvents);
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