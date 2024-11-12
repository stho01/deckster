using Deckster.Games;

namespace Deckster.Server.Data;

public interface IEventThing : IDisposable, IAsyncDisposable;

public interface IEventQueue<T> : IEventThing where T : GameObject
{
    void Append(object e);
    Task FlushAsync();
}