using Deckster.Server.Games;

namespace Deckster.Server.Data;

public interface IRepo
{
    Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : DatabaseObject;
    Task SaveAsync<T>(T item, CancellationToken cancellationToken = default) where T : DatabaseObject;
    IQueryable<T> Query<T>() where T : DatabaseObject;
    IEventThing<T> StartEventStream<T>(Guid id, IEnumerable<object> events) where T : GameObject;
    Task<T?> GetGameAsync<T>(Guid id, long version, CancellationToken cancellationToken = default) where T : GameObject;
}

public static class RepoExtensions
{
    public static IEventThing<T> StartEventStream<T>(this IRepo repo, Guid id, object startEvent) where T : GameObject
    {
        return repo.StartEventStream<T>(id, [startEvent]);
    }
}