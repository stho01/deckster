using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.Uno;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.TestGame;
using Deckster.Server.Games.Uno.Core;

namespace Deckster.Server.Games.Uno;

public class UnoGameHost : GameHost<UnoRequest,UnoResponse,UnoGameNotification>
{
    public event EventHandler<UnoGameHost>? OnEnded;

    public override string GameType => "Uno";
    public override GameState State => _game.State;
    public string Name { get; init; } = Guid.NewGuid().ToString();

    private readonly ConcurrentDictionary<Guid, IServerChannel> _players = new();
    private readonly UnoGame _game;
    private readonly CancellationTokenSource _cts = new();

    public UnoGameHost()
    {
        _game = new()
        {
            Id = Guid.NewGuid()
        };
    }
    
    private async void MessageReceived(PlayerData player, UnoRequest message)
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
        if (result is UnoSuccessResponse)
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

    private Task BroadcastMessageAsync(UnoGameNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(notification, JsonOptions, cancellationToken).AsTask()));
    }

    private async Task<UnoResponse> HandleRequestAsync(Guid id, UnoRequest message, IServerChannel player)
    {
        switch (message)
        {
            case PutCardRequest command:
            {
                var result = _game.PutCard(id, command.Card);
                await player.ReplyAsync(result, JsonOptions);
                return result;
            }
            case PutWildRequest command:
            {
                var result = _game.PutWild(id, command.Card, command.NewColor);
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
                var result = new UnoFailureResponse($"Unknown command '{message.Type}'");
                await player.ReplyAsync(result, JsonOptions);
                return result;
            }
        }
    }

    public override async Task Start()
    {
        _game.NewRound(DateTimeOffset.Now);
        foreach (var player in _players.Values)
        {
            player.Start<UnoRequest>(MessageReceived, JsonOptions, _cts.Token);
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

    public override ICollection<PlayerData> GetPlayers()
    {
        return _players.Values.Select(c => c.Player).ToArray();
    }
}