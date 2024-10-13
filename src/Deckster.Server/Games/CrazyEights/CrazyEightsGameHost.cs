using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Protocol;
using Deckster.Server.Communication;
using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGameHost : GameHost<CrazyEightsRequest, CrazyEightsResponse, CrazyEightsNotification>
{
    public event Action<IGameHost>? OnEnded;
    public override string GameType => "CrazyEights";
    public override GameState State => _game.State;

    private readonly CrazyEightsGame _game = new() { Id = Guid.NewGuid() };

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

    private async void MessageReceived(IServerChannel channel, CrazyEightsRequest request)
    {
        if (_game.State != GameState.Running)
        {
            await channel.ReplyAsync(new FailureResponse("Game is not running"), JsonOptions);
            return;
        }

        var result = HandleRequestAsync(channel, request);
        await channel.ReplyAsync(result, JsonOptions);
        if (result is CrazyEightsSuccessResponse)
        {
            if (_game.State == GameState.Finished)
            {
                await BroadcastMessageAsync(new GameEndedNotification());
                await Task.WhenAll(_players.Values.Select(p => p.WeAreDoneHereAsync()));
                await Cts.CancelAsync();
                Cts.Dispose();
                OnEnded?.Invoke(this);
                return;
            }
            var currentPlayerId = _game.CurrentPlayer.Id;
            await _players[currentPlayerId].PostMessageAsync(new ItsYourTurnNotification(), JsonOptions);
        }
    }

    private DecksterResponse HandleRequestAsync(IServerChannel channel, CrazyEightsRequest message)
    {
        return message switch
        {
            PutCardRequest request => _game.PutCard(channel.Player.Id, request.Card),
            PutEightRequest request => _game.PutEight(channel.Player.Id, request.Card, request.NewSuit),
            DrawCardRequest => _game.DrawCard(channel.Player.Id),
            PassRequest => _game.Pass(channel.Player.Id),
            _ => new CrazyEightsFailureResponse($"Unknown command '{message.Type}'")
        };
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
            player.Start<CrazyEightsRequest>(MessageReceived, JsonOptions, Cts.Token);
        }
        var currentPlayerId = _game.CurrentPlayer.Id;
        await _players[currentPlayerId].PostMessageAsync(new ItsYourTurnNotification(), JsonOptions);
    }
}