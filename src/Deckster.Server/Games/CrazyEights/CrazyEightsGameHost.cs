using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.CrazyEights.Core;
using Deckster.Server.Games.TestGame;

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

    private async void MessageReceived(PlayerData player, CrazyEightsRequest message)
    {
        if (!_players.TryGetValue(player.Id, out var channel))
        {
            return;
        }
        if (_game.State != GameState.Running)
        {
            await channel.ReplyAsync(new FailureResponse("Game is not running"), JsonOptions);
            return;
        }

        var result = await HandleRequestAsync(player.Id, message, channel);
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

    private Task BroadcastMessageAsync(CrazyEightsNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(notification, JsonOptions, cancellationToken).AsTask()));
    }

    private async Task<CrazyEightsResponse> HandleRequestAsync(Guid id, CrazyEightsRequest message, IServerChannel player)
    {
        switch (message)
        {
            case PutCardRequest request:
            {
                var result = _game.PutCard(id, request.Card);
                await player.ReplyAsync(result, JsonOptions);
                return result;
            }
            case PutEightRequest request:
            {
                var result = _game.PutEight(id, request.Card, request.NewSuit);
                await player.ReplyAsync(result, JsonOptions);
                return result;
            }
            case DrawCardRequest:
            {
                var result = _game.DrawCard(id);
                await player.ReplyAsync(result, JsonOptions);
                return result;
            }
            case PassRequest:
            {
                var result = _game.Pass(id);
                await player.ReplyAsync(result, JsonOptions);
                return result;
            }
            default:
            {
                var result = new CrazyEightsFailureResponse($"Unknown command '{message.Type}'");
                await player.ReplyAsync(result, JsonOptions);
                return result;
            }
        }
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