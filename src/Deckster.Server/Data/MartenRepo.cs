using Deckster.Games;
using Deckster.Games.Data;
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

    public async Task<Historic<T>?> GetGameAsync<T>(Guid id, long version, CancellationToken cancellationToken = default) where T : GameObject
    {
        var game = await _session.Events.AggregateStreamAsync<T>(id, version, token: cancellationToken);
        if (game == null)
        {
            return null;
        }
        var events = await _session.Events.FetchStreamAsync(id, version, token: cancellationToken);
        return new Historic<T>(game, events.Select(e => e.Data).ToList());
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

public abstract class HistoricGame
{
    public List<object> Events { get; }
    
    public HistoricGame(List<object> events)
    {
        Events = events;
    }

    public abstract GameObject GetGame();
}

public class Historic<TGame> : HistoricGame where TGame : GameObject
{
    public TGame Game { get; }
    
    public Historic(TGame game, List<object> events) : base(events)
    {
        Game = game;
    }

    public override GameObject GetGame()
    {
        return Game;
    }
}