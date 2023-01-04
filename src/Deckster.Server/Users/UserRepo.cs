using System.Collections.Concurrent;

namespace Deckster.Server.Users;

public class UserRepo
{
    private readonly ConcurrentDictionary<Guid, User> _users = new()
    {
        [Guid.Parse("eed69907-916a-47fb-bc3c-a96dd096e64d")] = new User
        {
            Id = Guid.Parse("eed69907-916a-47fb-bc3c-a96dd096e64d"),
            AccessToken = "abc123",
            Name = "Kamuf Larsen"
        }
    };

    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = _users.TryGetValue(id, out var u) ? u : null;
        return Task.FromResult(user);
    }


    public Task<User?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u => u.AccessToken == token);
        return Task.FromResult(user);
    }
}