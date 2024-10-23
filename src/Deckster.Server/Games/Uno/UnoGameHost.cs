using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.Uno;
using Deckster.Server.Communication;
using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.Common.Fakes;
using Deckster.Server.Games.Uno.Core;
using Deckster.Uno.SampleClient;

namespace Deckster.Server.Games.Uno;

public class UnoGameHost : GameHost<UnoRequest,UnoResponse,UnoGameNotification>
{
    public event EventHandler<UnoGameHost>? OnEnded;

    public override string GameType => "Uno";
    public override GameState State => _game.State;

    private readonly UnoGame _game;
    private readonly List<UnoNoob> _bots = [];

    public UnoGameHost()
    {
        _game = new()
        {
            Id = Guid.NewGuid()
        };
    }
    
    private async void MessageReceived(IServerChannel channel, UnoRequest message)
    {
        if (_game.State != GameState.Running)
        {
            await channel.ReplyAsync(new FailureResponse("Game is not running"), JsonOptions);
            return;
        }

        var result = await HandleRequestAsync(channel, message);
        if (result is UnoSuccessResponse)
        {
            if (_game.State == GameState.Finished)
            {
                await NotifyAllAsync(new GameEndedNotification());
                await Task.WhenAll(_players.Values.Select(p => p.DisconnectAsync()));
                await Cts.CancelAsync();
                Cts.Dispose();
                OnEnded?.Invoke(this, this);
                return;
            }
            var currentPlayerId = _game.CurrentPlayer.Id;
            await _players[currentPlayerId].SendNotificationAsync(new ItsYourTurnNotification(), JsonOptions);
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
    
    public override bool TryAddBot([MaybeNullWhen(true)] out string error)
    {
        var channel = new InMemoryChannel
        {
            Player = new PlayerData
            {
                Id = Guid.NewGuid(),
                Name = TestNames.Random()
            }
        };
        var bot = new UnoNoob(new UnoClient(channel));
        _bots.Add(bot);
        return TryAddPlayer(channel, out error);
    }

    private async Task<UnoResponse> HandleRequestAsync(IServerChannel channel, UnoRequest message)
    {
        switch (message)
        {
            case PutCardRequest command:
            {
                var result = _game.PutCard(channel.Player.Id, command.Card);
                await channel.ReplyAsync(result, JsonOptions);
                return result;
            }
            case PutWildRequest command:
            {
                var result = _game.PutWild(channel.Player.Id, command.Card, command.NewColor);
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
                var result = new UnoFailureResponse($"Unknown command '{message.Type}'");
                await channel.ReplyAsync(result, JsonOptions);
                return result;
            }
        }
    }

    public override async Task StartAsync()
    {
        _game.NewRound(DateTimeOffset.Now);
        foreach (var player in _players.Values)
        {
            player.Start<UnoRequest>(MessageReceived, JsonOptions, Cts.Token);
        }
        var currentPlayerId = _game.CurrentPlayer.Id;
        await _players[currentPlayerId].SendNotificationAsync(new ItsYourTurnNotification(), JsonOptions);
    }
}