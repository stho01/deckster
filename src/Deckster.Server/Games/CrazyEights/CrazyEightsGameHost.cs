using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Protocol;
using Deckster.Server.Communication;
using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGameHost : IGameHost
{
    public event EventHandler<CrazyEightsGameHost> OnEnded;

    public Guid Id => _game.Id;

    private readonly ConcurrentDictionary<Guid, IServerChannel> _players = new();
    private readonly CrazyEightsGame _game = new() { Id = Guid.NewGuid() };
    private readonly CancellationTokenSource _cts = new();

    private async void MessageReceived(PlayerData player, DecksterRequest message)
    {
        if (!_players.TryGetValue(player.PlayerId, out var channel))
        {
            return;
        }
        if (_game.State != GameState.Running)
        {
            await channel.ReplyAsync(new FailureResponse("Game is not running"));
            return;
        }

        var result = await HandleRequestAsync(player.PlayerId, message, channel);
        if (result is SuccessResponse)
        {
            if (_game.State == GameState.Finished)
            {
                await BroadcastMessageAsync(new GameEndedMessage());
                await Task.WhenAll(_players.Values.Select(p => p.WeAreDoneHereAsync()));
                await _cts.CancelAsync();
                _cts.Dispose();
                OnEnded?.Invoke(this, this);
                return;
            }
            var currentPlayerId = _game.CurrentPlayer.Id;
            await _players[currentPlayerId].PostMessageAsync(new ItsYourTurnMessage());
        }
    }

    public bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (!_game.TryAddPlayer(channel.Player.PlayerId, channel.Player.Name, out error))
        {
            error = "Could not add player";
            return false;
        }

        if (!_players.TryAdd(channel.Player.PlayerId, channel))
        {
            error = "Could not add player";
            return false;
        }
        
        

        error = default;
        return true;
    }

    private Task BroadcastMessageAsync(DecksterMessage message, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(message, cancellationToken).AsTask()));
    }

    private async Task<DecksterResponse> HandleRequestAsync(Guid id, DecksterRequest message, IServerChannel player)
    {
        switch (message)
        {
            case PutCardRequest command:
            {
                var result = _game.PutCard(id, command.Card);
                await player.ReplyAsync(result);
                return result;
            }
            case PutEightRequest command:
            {
                var result = _game.PutEight(id, command.Card, command.NewSuit);
                await player.ReplyAsync(result);
                return result;
            }
            case DrawCardRequest:
            {
                var result = _game.DrawCard(id);
                await player.ReplyAsync(result);
                return result;
            }
            case PassRequest:
            {
                var result = _game.Pass(id);
                await player.ReplyAsync(result);
                return result;
            }
            default:
            {
                var result = new FailureResponse($"Unknown command '{message.Type}'");
                await player.ReplyAsync(result);
                return result;
            }
        }
    }

    public async Task Start()
    {
        _game.Reset();
        foreach (var player in _players.Values)
        {
            player.Received += MessageReceived;
        }
        var currentPlayerId = _game.CurrentPlayer.Id;
        await _players[currentPlayerId].PostMessageAsync(new ItsYourTurnMessage());
    }
    
    public async Task CancelAsync(string reason)
    {
        foreach (var player in _players.Values.ToArray())
        {
            player.Received -= MessageReceived;
            await player.DisconnectAsync(true, reason);
            player.Dispose();
        }
    }
}