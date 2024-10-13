using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Server.Communication;
using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGameHost : GameHost<CrazyEightsRequest, CrazyEightsResponse, CrazyEightsNotification>
{
    public event EventHandler<IGameHost>? OnEnded;

    public override string GameType => "CrazyEights";
    public override GameState State => _game.State;

    protected readonly ConcurrentDictionary<Guid, IServerChannel> _players = new();
    private readonly CrazyEightsGame _game = new() { Id = Guid.NewGuid() };
    private readonly CancellationTokenSource _cts = new();
    
    public override ICollection<PlayerData> GetPlayers()
    {
        return _players.Values.Select(c => c.Player).ToArray();
    }
    
    public override bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (!_game.TryAddPlayer(channel.Player.Id, channel.Player.Name, out error))
        {
            error = "Could not add player";
            return false;
        }

        if (!_players.TryAdd(channel.Player.Id, channel))
        {
            error = "Could not add player";
            return false;
        }

        error = default;
        return true;
    }

    private async void MessageReceived(IServerChannel channel, CrazyEightsRequest message)
    {
        if (_game.State != GameState.Running)
        {
            await channel.ReplyAsync(new FailureResponse("Game is not running"), JsonOptions);
            return;
        }

        var result = await HandleRequestAsync(channel, message);
        if (result is CrazyEightsSuccessResponse)
        {
            if (_game.State == GameState.Finished)
            {
                await BroadcastMessageAsync(new GameEndedNotification());
                await Task.WhenAll(_players.Values.Select(p => p.WeAreDoneHereAsync()));
                await _cts.CancelAsync();
                _cts.Dispose();
                OnEnded?.Invoke(this, this);
                return;
            }
            var currentPlayerId = _game.CurrentPlayer.Id;
            await _players[currentPlayerId].PostMessageAsync(new ItsYourTurnNotification(), JsonOptions);
        }
    }

    private async Task<CrazyEightsResponse> HandleRequestAsync(IServerChannel channel, CrazyEightsRequest message)
    {
        switch (message)
        {
            case PutCardRequest request:
            {
                var result = _game.PutCard(channel.Player.Id, request.Card);
                await channel.ReplyAsync(result, JsonOptions);
                return result;
            }
            case PutEightRequest request:
            {
                var result = _game.PutEight(channel.Player.Id, request.Card, request.NewSuit);
                await channel.ReplyAsync(result, JsonOptions);
                return result;
            }
            case DrawCardRequest:
            {
                var result = _game.DrawCard(channel.Player.Id);
                await channel.ReplyAsync(result, JsonOptions);
                return result;
            }
            case PassRequest:
            {
                var result = _game.Pass(channel.Player.Id);
                await channel.ReplyAsync(result, JsonOptions);
                return result;
            }
            default:
            {
                var result = new CrazyEightsFailureResponse($"Unknown command '{message.Type}'");
                await channel.ReplyAsync(result, JsonOptions);
                return result;
            }
        }
    }
    
    private Task BroadcastMessageAsync(CrazyEightsNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(notification, JsonOptions, cancellationToken).AsTask()));
    }

    public override async Task Start()
    {
        if (_game.State != GameState.Waiting)
        {
            return;
        }
        _game.Reset();
        foreach (var player in _players.Values)
        {
            player.Start<CrazyEightsRequest>(MessageReceived, JsonOptions, _cts.Token);
        }
        var currentPlayerId = _game.CurrentPlayer.Id;
        await _players[currentPlayerId].PostMessageAsync(new ItsYourTurnNotification(), JsonOptions);
    }
    
    public override async Task CancelAsync()
    {
        await _cts.CancelAsync();
        foreach (var player in _players.Values.ToArray())
        {
            await player.DisconnectAsync();
            player.Dispose();
        }
    }
}