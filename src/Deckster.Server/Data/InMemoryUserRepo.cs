using System.Collections;
using System.Collections.Concurrent;

namespace Deckster.Server.Data;

public class InMemoryRepo : IRepo
{
    private readonly ConcurrentDictionary<Type, IDictionary> _collections = new();

    public InMemoryRepo()
    {
        _collections[typeof(DecksterUser)] = new ConcurrentDictionary<Guid, DecksterUser>
        {
            [Guid.Parse("eed69907-916a-47fb-bc3c-a96dd096e64d")] = new()
            {
                Id = Guid.Parse("eed69907-916a-47fb-bc3c-a96dd096e64d"),
                Password = "hest",
                AccessToken = "abc123",
                Name = "Kamuf Larsen"
            }
        }; 
    }

    public IQueryable<T> Query<T>() where T : DatabaseObject
    {
        return GetCollection<T>().Values.AsQueryable();
    }

    public Task SaveAsync<T>(T item, CancellationToken cancellationToken = default) where T : DatabaseObject
    {
        if (item.Id == default)
        {
            item.Id = Guid.NewGuid();
        }

        var collection = GetCollection<T>();
        
        collection.AddOrUpdate(item.Id, item, (_, _) => item);
        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : DatabaseObject
    {
        var collection = GetCollection<T>();
        var item = collection.GetValueOrDefault(id);
        return Task.FromResult(item);
    }

    private ConcurrentDictionary<Guid, T> GetCollection<T>() where T : DatabaseObject
    {
        var collection = _collections.GetOrAdd(typeof(T), _ => new ConcurrentDictionary<Guid, T>());
        return (ConcurrentDictionary<Guid, T>) collection;
    }
}