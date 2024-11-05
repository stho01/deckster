using Deckster.Client.Games.Gabong;
using Deckster.Client.Logging;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Gabong;
using Microsoft.Extensions.Logging;

namespace Deckster.Gabong.SampleClient;

public class GabongPoorAi
{
    private readonly ILogger _logger;
    
    private PlayerViewOfGame _view = new();
    private readonly TaskCompletionSource _tcs = new();

    private readonly GabongClient _client;

    public GabongPoorAi(GabongClient client)
    {
        _client = client;
        _logger = Log.Factory.CreateLogger(client.PlayerData.Name);
        client.GameStarted += OnGameStarted;
        client.RoundStarted += OnRoundStarted;
        client.ItsYourTurn += OnItsYourTurn;
        client.PlayerPutCard += OnPlayerPutCard;
        client.PlayerPutWild += OnPlayerPutWild;
        client.PlayerDrewCard += OnPlayerDrewCard;
        client.PlayerPassed += OnPlayerPassed;
        client.RoundEnded += OnRoundEnded;
        client.GameEnded += OnGameEnded;
    }

    private void OnRoundStarted(RoundStartedNotification obj)
    {
        _view = obj.PlayerViewOfGame;
    }

    private void OnGameStarted(GameStartedNotification obj)
    {
        _view = obj.PlayerViewOfGame;
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
    }

    private void OnPlayerPassed(PlayerPassedNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} passed");
    }

    private OtherGabongPlayer? GetPlayer(Guid playerId)
    {
        return _view.OtherPlayers.FirstOrDefault(p => p.Id == playerId);
    }

    private void OnPlayerDrewCard(PlayerDrewCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Drew");
    }

    private void OnPlayerPutWild(PlayerPutWildNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Played {obj.Card} and changed suit to {obj.NewSuit}");
    }

    private void OnPlayerPutCard(PlayerPutCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Played {obj.Card}");
    }

    private async void OnItsYourTurn(ItsYourTurnNotification notification)
    {
        try
        {
            var cards = notification.PlayerViewOfGame.Cards;
            
            _logger.LogDebug("It's my turn. Top: {top} ({suit}). I have: {cards}",
                notification.PlayerViewOfGame.TopOfPile,
                notification.PlayerViewOfGame.CurrentSuit,
                string.Join(", ", cards));
            
            _view = notification.PlayerViewOfGame;
        
            if (TryGetCard(out var card))
            {
                try
                {
                    _logger.LogInformation("Putting card: {card}", card);
                    var r = await _client.PutCardAsync(card);
                    _logger.LogDebug("Result: {result}", r.GetType().Name);
                }
                catch (Exception e)
                {
                    _logger.LogError("{message}", e.Message);
                    throw;
                }

                return;
            }

            card = await _client.DrawCardAsync();
            _view.Cards.Add(card);
            if (TryGetCard(out card))
            {
                try
                {
                    _logger.LogInformation("Putting card: {card}", card);
                    var r = await _client.PutCardAsync(card);
                    _logger.LogDebug("Result: {result}", r.GetType().Name);
                    return;
                }
                catch (Exception e)
                {
                    _logger.LogError("{message}", e.Message);
                    throw;
                }

                return;
            }
        
            _logger.LogInformation("Passing");
            await _client.PassAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Argh");
            throw;
        }
        _logger.LogDebug("Done");
    }
    
    private bool TryGetCard(out Card card)
    {
        _logger.LogTrace("Try get card");
        card = default;
        foreach (var c in _view.Cards.Where(c => c.Suit == _view.TopOfPile.Suit || c.Suit == _view.CurrentSuit))
        {
            card = c;
            return true;
        }
        _logger.LogTrace("Try get card: no");
        return false;
    }
}