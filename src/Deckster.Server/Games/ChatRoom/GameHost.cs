using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.ChatRoom;

public abstract class GameHost : IGameHost, ICommunicationContext
{
    public event Action<IGameHost>? OnEnded;
    
    public abstract string GameType { get; }
    public string Name { get; set; }
    public abstract GameState State { get; }
    private readonly TaskCompletionSource<Guid?> _tcs = new();
    
    protected readonly ConcurrentDictionary<Guid, IServerChannel> Players = new();
    protected readonly CancellationTokenSource Cts = new();

    protected readonly JsonSerializerOptions JsonOptions = DecksterJson.Options;

    public abstract Task StartAsync();

    public abstract bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error);
    public abstract bool TryAddBot([MaybeNullWhen(true)] out string error);

    
    
    public ICollection<PlayerData> GetPlayers()
    {
        return Players.Values.Select(c => c.Player).ToArray();
    }

    public Task NotifyAllAsync(DecksterNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(Players.Values.Select(p => p.SendNotificationAsync(notification, JsonOptions, cancellationToken).AsTask()));
    }

    public async Task RespondAsync(Guid playerId, DecksterResponse response, CancellationToken cancellationToken = default)
    {
        if (Players.TryGetValue(playerId, out var channel))
        {
            await channel.ReplyAsync(response, JsonOptions, cancellationToken);
        }
    }

    public async Task NotifyAsync(Guid playerId, DecksterNotification notification, CancellationToken cancellationToken = default)
    {
        if (Players.TryGetValue(playerId, out var channel))
        {
            await channel.SendNotificationAsync(notification, JsonOptions, cancellationToken);
        }
    }

    public Task EndAsync() => EndAsync(null);
    
    protected async Task EndAsync(Guid? gameId)
    {
        if (!Cts.IsCancellationRequested)
        {
            await Cts.CancelAsync();    
        }
        foreach (var player in Players.Values.ToArray())
        {
            await player.DisconnectAsync();
            player.Dispose();
        }
        Players.Clear();
        FireEnded(gameId);
    }

    private void FireEnded(Guid? gameId)
    {
        _tcs.TrySetResult(gameId);
        if (OnEnded == null)
        {
            return;
        }
        var onEnded = OnEnded;
        OnEnded = null;
        onEnded?.Invoke(this);
    }

    public async Task<Guid?> RunAsync()
    {
        await StartAsync();
        return await _tcs.Task;
    }

    public override string ToString()
    {
        return $"{GameType} {Name}";
    }
}

