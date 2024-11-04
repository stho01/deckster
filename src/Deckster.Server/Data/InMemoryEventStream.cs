using Deckster.Games;
using Deckster.Server.Games;

namespace Deckster.Server.Data;

public class InMemoryEventQueue<T> : IEventQueue<T> where T : GameObject
{
    public Guid Id { get; }
    public List<object> Events { get; } = [];

    public InMemoryEventQueue(Guid id, IEnumerable<object> startEvents)
    {
        Id = id;
        Events.AddRange(startEvents);
    }
    
    public void Append(object e)
    {
        Events.Add(e);
    }

    public Task FlushAsync()
    {
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}