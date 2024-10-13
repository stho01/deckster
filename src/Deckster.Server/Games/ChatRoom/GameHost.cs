using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Deckster.Client.Common;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.ChatRoom;

public abstract class GameHost<TRequest, TResponse, TNotification> : IGameHost
    where TRequest : DecksterRequest
    where TResponse : DecksterResponse
    where TNotification : DecksterNotification
{
    public event Action<IGameHost>? OnEnded;
    
    public abstract string GameType { get; }
    public string Name { get; init; }
    public abstract GameState State { get; }

    protected readonly JsonSerializerOptions JsonOptions = DecksterJson.Create(o =>
    {
        o.AddAll<TRequest>().AddAll<TResponse>().AddAll<TNotification>();
    });

    protected readonly ConcurrentDictionary<Guid, IServerChannel> _players = new();
    protected readonly CancellationTokenSource Cts = new();

    public abstract Task Start();

    public abstract bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error);
    

    public ICollection<PlayerData> GetPlayers()
    {
        return _players.Values.Select(c => c.Player).ToArray();
    }

    protected Task BroadcastMessageAsync(DecksterNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(notification, JsonOptions, cancellationToken).AsTask()));
    }

    public async Task CancelAsync()
    {
        await Cts.CancelAsync();
        foreach (var player in _players.Values.ToArray())
        {
            await player.DisconnectAsync();
            player.Dispose();
        }
    }
}

