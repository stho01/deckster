using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using Deckster.Core.Communication.Handshake;
using Deckster.Core.Communication.WebSockets;
using Deckster.Core.Games.Common;
using Deckster.Core.Serialization;
using Deckster.Server.Communication;
using Deckster.Server.Data;
using Deckster.Server.Games.CrazyEights;

namespace Deckster.Server.Games;

public class GameHostRegistry
{
    private readonly ConcurrentDictionary<Guid, ConnectingPlayer> _connectingPlayers = new();
    private readonly ConcurrentDictionary<Type, IGameHostCollection> _collections = new();
    private readonly ILogger<GameHostRegistry> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public GameHostRegistry(IHostApplicationLifetime lifetime, ILogger<GameHostRegistry> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        lifetime.ApplicationStopping.Register(ApplicationStopping);
    }

    public void Add<TGameHost>(TGameHost host) where TGameHost : IGameHost
    {
        var games = GetCollection<TGameHost>();
        games.TryAdd(host.Name, host);
    }

    private GameHostCollection<TGameHost> GetCollection<TGameHost>() where TGameHost : IGameHost
    {
        var dictionary = _collections.GetOrAdd(typeof(TGameHost), _ => new GameHostCollection<TGameHost>());
        return (GameHostCollection<TGameHost>) dictionary;
    }
    
    public IEnumerable<TGameHost> GetHosts<TGameHost>() where TGameHost : IGameHost
    {
        return GetCollection<TGameHost>().Values;
    }

    public bool TryGet<TGameHost>(string id, [MaybeNullWhen(false)] out TGameHost o) where TGameHost : IGameHost
    {
        return GetCollection<TGameHost>().TryGetValue(id, out o);
    }

    public async Task<bool> StartJoinAsync<TGameHost>(DecksterUser user, WebSocket actionSocket, string gameName) where TGameHost : IGameHost
    {
        if (!GetCollection<TGameHost>().TryGetValue(gameName, out var host))
        {
            await actionSocket.SendMessageAsync(new ConnectFailureMessage
            {
                ErrorMessage = $"Unknown game '{gameName}'" 
            }, DecksterJson.Options);
            return false;
        }

        var player = new PlayerData
        {
            Name = user.Name,
            Id = user.Id
        };
        var connectingPlayer = new ConnectingPlayer(player, actionSocket, host);
        if (!_connectingPlayers.TryAdd(connectingPlayer.ConnectionId, connectingPlayer))
        {
            await actionSocket.SendMessageAsync(new ConnectFailureMessage
            {
                ErrorMessage = "ConnectionId conflict"
            }, DecksterJson.Options);
            return false;
        }

        await actionSocket.SendMessageAsync(new HelloSuccessMessage
        {
            ConnectionId = connectingPlayer.ConnectionId,
            Player = connectingPlayer.Player
        }, DecksterJson.Options);

        await connectingPlayer.TaskCompletionSource.Task;
        return true;
    }
    
    public async Task<bool> FinishJoinAsync(Guid connectionId, WebSocket eventSocket)
    {
        if (!_connectingPlayers.TryRemove(connectionId, out var connecting))
        {
            await eventSocket.SendMessageAsync<ConnectMessage>(new ConnectFailureMessage
            {
                ErrorMessage = $"Invalid connectionId: '{connectionId}'"
            }, DecksterJson.Options);
            return false;
        }
        
        var channel = new WebSocketServerChannel(connecting.Player, connecting.ActionSocket, eventSocket, connecting.TaskCompletionSource, _loggerFactory.CreateLogger<WebSocketServerChannel>());
        if (!connecting.GameHost.TryAddPlayer(channel, out var error))
        {
            await eventSocket.SendMessageAsync<ConnectMessage>(new ConnectFailureMessage
            {
                ErrorMessage = error
            }, DecksterJson.Options);
            await channel.DisconnectAsync();
            channel.Dispose();
            return false;
        }
        
        await eventSocket.SendMessageAsync<ConnectMessage>(new ConnectSuccessMessage(), DecksterJson.Options);
        await connecting.TaskCompletionSource.Task;
        return true;
    }
    
    private async void ApplicationStopping()
    {
        foreach (var connecting in _connectingPlayers.Values.ToArray())
        {
            await connecting.CancelAsync();
        }

        await Task.WhenAll(_collections.Values.SelectMany(c => c.GetValues()).Select(v => v.EndAsync()));
    }
}