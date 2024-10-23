using Deckster.Server.Games;

namespace Deckster.Server.Data;

public interface IEventThing : IDisposable, IAsyncDisposable;

public interface IEventThing<T> : IEventThing where T : GameObject
{
    void Append(object e);
    Task SaveChangesAsync();
}