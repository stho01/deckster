using System.Collections.Concurrent;
using Deckster.Games;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Marten;

namespace Deckster.Server.Data;

public class MartenEventQueue<T> : IEventQueue<T> where T : GameObject
{
    private readonly Guid _id;
    private readonly IDocumentSession _session;
    private readonly ConcurrentQueue<object> _events = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly object[] _startEvents;
    private bool _initiated;

    public MartenEventQueue(Guid id, IDocumentSession session, IEnumerable<object> startEvents)
    {
        _id = id;
        _session = session;
        _startEvents = startEvents.ToArray();
    }

    public void Append(object e)
    {
        _events.Enqueue(e);
    }

    public async Task FlushAsync()
    {
        await EnsureInitiatedAsync();
        while (_events.TryDequeue(out var e))
        {
            _session.Events.Append(_id, e);
            await _session.SaveChangesAsync();
        }
    }

    private async ValueTask EnsureInitiatedAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_initiated)
            {
                return;
            }
            var stream = _session.Events.StartStream<T>(_id, _startEvents);
            await _session.SaveChangesAsync();
            _initiated = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _session.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
        await _session.DisposeAsync();
    }
}