using Deckster.Client.Games.Gabong;
using Deckster.Client.Logging;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.CrazyEights;
using Deckster.Core.Games.Gabong;
using Microsoft.Extensions.Logging;
using DrawCardRequest = Deckster.Core.Games.Gabong.DrawCardRequest;
using GameEndedNotification = Deckster.Core.Games.Gabong.GameEndedNotification;
using GameStartedNotification = Deckster.Core.Games.Gabong.GameStartedNotification;
using PlayerDrewCardNotification = Deckster.Core.Games.Gabong.PlayerDrewCardNotification;
using PlayerPutCardNotification = Deckster.Core.Games.Gabong.PlayerPutCardNotification;
using PlayerViewOfGame = Deckster.Core.Games.Gabong.PlayerViewOfGame;
using PutCardRequest = Deckster.Core.Games.Gabong.PutCardRequest;

namespace Deckster.Gabong.SampleClient;

public class GabongPoorAi
{
    private readonly ILogger _logger;
    
    private PlayerViewOfGame _view = new();
    private readonly TaskCompletionSource _tcs = new();

    private readonly GabongClient _client;

    private bool _weArePlaying = false;
    
    private Task _playTask = Task.CompletedTask;
    
    public GabongPoorAi(GabongClient client)
    {
        _client = client;
        _logger = Log.Factory.CreateLogger(client.PlayerData.Name);
        client.GameStarted += OnGameStarted;
        client.RoundStarted += OnRoundStarted;
        client.PlayerPutCard += OnPlayerPutCard;
        client.PlayerDrewCard += OnPlayerDrewCard;
        client.PlayerLostTheirTurn += OnPlayerLostTheirTurn;
        client.PlayerDrewPenaltyCard += OnPlayerDrewPenaltyCard;
        client.RoundEnded += OnRoundEnded;
        client.GameEnded += OnGameEnded;
    }

    private void OnPlayerDrewPenaltyCard(PlayerDrewPenaltyCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Drew a penalty card");
    }

    private void OnRoundStarted(RoundStartedNotification obj)
    {
        _view = obj.PlayerViewOfGame;
        _weArePlaying = true;
    }

    private void OnGameStarted(GameStartedNotification obj)
    {
        Console.WriteLine("==> Game started");
        _playTask = Task.Run(async () =>
        {
            while (!_tcs.Task.IsCompleted)
            {
                await ThinkAboutDoingSomething(_view);
                await Task.Delay(500+new Random().Next(1000));
            }
        });
    }

    public Task PlayAsync() => _tcs.Task;

    private void OnGameEnded(GameEndedNotification obj)
    {
        Console.WriteLine("==> Game ended");
        _tcs.SetResult();
    }

    private void OnRoundEnded(RoundEndedNotification obj)
    {
        Console.WriteLine("==> Round ended");
        _weArePlaying = false;
    }

    private void OnPlayerLostTheirTurn(PlayerLostTheirTurnNotification obj)
    {
        _view.LastPlay = GabongPlay.TurnLost;
        _view.LastPlayMadeByPlayerId = obj.PlayerId;
        
        var lostTurnReason = obj.LostTurnReason switch
        {
            PlayerLostTurnReason.FinishedDrawingCardDebt => "drew their card debt",
            PlayerLostTurnReason.Passed => "passed",
            PlayerLostTurnReason.WrongPlay => "made a wrong play",
            PlayerLostTurnReason.TookTooLong => "took too long",
            _ => "unknown"
        };
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} lost their turn since they {lostTurnReason}");
    }

    private OtherGabongPlayer? GetPlayer(Guid playerId)
    {
        return _view.OtherPlayers.FirstOrDefault(p => p.Id == playerId);
    }

    private void OnPlayerDrewCard(PlayerDrewCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Drew");
    }
    
    private void OnPlayerPutCard(PlayerPutCardNotification evt)
    {
        _view.TopOfPile = evt.Card;
        _view.CurrentSuit = evt.NewSuit ?? evt.Card.Suit;
        _view.LastPlay = GabongPlay.CardPlayed;
        _view.LastPlayMadeByPlayerId = evt.PlayerId;

        var newSuitText = evt.NewSuit == null ? "" : $" and changed suit to {evt.NewSuit}";
        Console.WriteLine($"==> {GetPlayer(evt.PlayerId)} Played {evt.Card} {newSuitText}");
    }

    private async Task ThinkAboutDoingSomething(PlayerViewOfGame? obj)
    {
        if (!_weArePlaying)
        {
            return;
        }
        if (obj == null)
        {
            return;
        }
        if(IBelieveItsMyTurn(obj))
        {
            _logger.LogDebug("i believe it's my turn. Top: {top} ({suit}). I have: {cards}",
                _view.TopOfPile,
                _view.CurrentSuit,
                string.Join(", ", _view.Cards));
            await DoSomePlay(obj);
        }
    }

    private async Task DoSomePlay(PlayerViewOfGame viewOfGame)
    {
        try{
            var cardToPlay = FindCardToPlay(viewOfGame);
            if(cardToPlay != null)
            {
                var canChangeSuit = cardToPlay.Value.Rank == 8;
                var newSuit = canChangeSuit ? viewOfGame.Cards.GroupBy(x=>x.Suit).OrderByDescending(x=>x.Count()).First().Key : (Suit?)null;
                _logger.LogInformation("Trying to play card {card}", cardToPlay.Value);
                _view = await _client.PutCardAsync(new PutCardRequest{Card = cardToPlay.Value, NewSuit = newSuit});
            }
            else
            {
                _view = await _client.DrawCardAsync(new DrawCardRequest());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Argh");
            throw;
        }
        _logger.LogDebug("Done");
    }

 
    private bool IBelieveItsMyTurn(PlayerViewOfGame viewOfGame)
    {
        var myIndex = viewOfGame.PlayersOrder.IndexOf(_client.PlayerData.Id);
        var lastplayer = viewOfGame.PlayersOrder.IndexOf(viewOfGame.LastPlayMadeByPlayerId);
        if (viewOfGame.LastPlay == GabongPlay.RoundStarted && lastplayer == myIndex)
        {
            return true;
        }
        if(myIndex - lastplayer == 1)
        {
            return true;
        }
        if(myIndex == 0 && lastplayer == viewOfGame.PlayersOrder.Count - 1)
        {
            return true;
        }

        return false;
    }

    private Card? FindCardToPlay(PlayerViewOfGame viewOfGame)
    {
        foreach (var c in _view.Cards.Where(c => c.Suit == _view.CurrentSuit || c.Rank == _view.TopOfPile.Rank))
        {
            return c;
        }
        return null;
    }

}