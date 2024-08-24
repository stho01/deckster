namespace Deckster.Server.Data;

public interface IRepo
{
    Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : DatabaseObject;
    Task SaveAsync<T>(T item, CancellationToken cancellationToken = default) where T : DatabaseObject;
    IQueryable<T> Query<T>() where T : DatabaseObject;
}