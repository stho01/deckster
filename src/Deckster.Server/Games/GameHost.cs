using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;
using Deckster.Core.Serialization;
using Deckster.Games;
using Deckster.Server.Communication;

namespace Deckster.Server.Games;

public abstract class GameHost : IGameHost
{
    protected readonly int? PlayerLimit;
    
    public event Action<IGameHost>? OnEnded;
    
    protected readonly SemaphoreSlim _semaphore = new(1, 1);
    protected readonly SemaphoreSlim _endSemaphore = new(1, 1);
    
    public abstract string GameType { get; }
    public string Name { get; set; }
    public abstract GameState State { get; }
    private readonly TaskCompletionSource<Guid?> _tcs = new();
    
    protected readonly ConcurrentDictionary<Guid, IServerChannel> Players = new();
    protected readonly CancellationTokenSource Cts = new();

    protected readonly JsonSerializerOptions JsonOptions = DecksterJson.Options;

    protected GameHost(int? playerLimit)
    {
        PlayerLimit = playerLimit;
    }

    public abstract Task StartAsync();

    public bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (PlayerLimit.HasValue && Players.Count >= PlayerLimit.Value)
        {
            error = "Too many players";
            return false;
        }

        if (!Players.TryAdd(channel.Player.Id, channel))
        {
            error = "Could not add player";
            return false;
        }

        channel.StartReading<DecksterRequest>(RequestReceived, JsonOptions, Cts.Token);
        channel.Disconnected += ChannelDisconnected;

        error = default;
        return true;
    }

    protected abstract void RequestReceived(IServerChannel channel, DecksterRequest request);
    protected abstract void ChannelDisconnected(IServerChannel channel, DisconnectReason readon);
    public abstract bool TryAddBot([MaybeNullWhen(true)] out string error);
    
    public List<PlayerData> GetPlayers()
    {
        return Players.Values.Select(c => c.Player).ToList();
    }

    public Task NotifyAllAsync(DecksterNotification notification)
    {
        return Task.WhenAll(Players.Values.Select(p => p.SendNotificationAsync(notification, JsonOptions, Cts.Token).AsTask()));
    }

    public async Task RespondAsync(Guid playerId, DecksterResponse response)
    {
        if (Players.TryGetValue(playerId, out var channel))
        {
            await channel.ReplyAsync(response, JsonOptions, Cts.Token);
        }
    }

    public async Task NotifyPlayerAsync(Guid playerId, DecksterNotification notification)
    {
        if (Players.TryGetValue(playerId, out var channel))
        {
            await channel.SendNotificationAsync(notification, JsonOptions, Cts.Token);
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
        try
        {
            onEnded?.Invoke(this);
        }
        catch
        {
            // ¯\_(ツ)_/¯
        }
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
