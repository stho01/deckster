using Deckster.Server.Games;
using Marten;

namespace Deckster.Server.Data;

public class MartenEventThing<T> : IEventThing<T> where T : GameObject
{
    private readonly Guid _id;
    private readonly IDocumentSession _session;

    public MartenEventThing(Guid id, IDocumentSession session)
    {
        _id = id;
        _session = session;
    }

    public void Append(object e)
    {
        _session.Events.Append(_id, e);
    }

    public async Task SaveChangesAsync()
    {
        var item = await _session.Events.AggregateStreamAsync<T>(_id);
        if (item != null)
        {
            _session.Store(item);    
        }
        await _session.SaveChangesAsync();
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