using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Deckster.Server.Games;

public class GameHostCollection<TGameHost> : IGameHostCollection where TGameHost : IGameHost
{
    private readonly ConcurrentDictionary<string, TGameHost> _hosts = new();

    public ICollection<TGameHost> Values => _hosts.Values;
    public IEnumerable<IGameHost> GetValues() => _hosts.Values.Cast<IGameHost>();
    
    public bool TryAdd(string name, TGameHost host)
    {
        if (_hosts.TryAdd(name, host))
        {
            host.OnEnded += Remove;
            return true;
        }

        return false;
    }

    private async void Remove(IGameHost host)
    {
        if (_hosts.TryRemove(host.Name, out _))
        {
            await host.EndAsync();
        }
    }

    public bool TryGetValue(string name, [MaybeNullWhen(false)] out TGameHost host) => _hosts.TryGetValue(name, out host);
}