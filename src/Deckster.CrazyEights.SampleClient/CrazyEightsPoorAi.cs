using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Logging;
using Microsoft.Extensions.Logging;

namespace Deckster.CrazyEights.SampleClient;

public class CrazyEightsPoorAi
{
    private readonly ILogger _logger;
    
    private PlayerViewOfGame _view = new();
    private readonly CrazyEightsClient _client;
    private bool _gameEnded;

    public CrazyEightsPoorAi(CrazyEightsClient client)
    {
        _client = client;
        _logger = Log.Factory.CreateLogger(client.PlayerData.Name);
        client.PlayerPassed += PlayerPassed;
        client.PlayerDrewCard += PlayerDrewCard;
        client.PlayerPutCard += PlayerPutCard;
        client.PlayerPutEight += PlayerPutEight;
        client.ItsYourTurn += ItsMyTurn;
        client.GameStarted += GameStarted;
        client.GameEnded += GameEnded;
    }

    private void GameEnded(GameEndedMessage message)
    {
        _logger.LogInformation($"Game ended. Players: [{string.Join(", ", message.Players.Select(p => p.Name))}]");
        _gameEnded = true;
    }

    private void GameStarted(GameStartedMessage message)
    {
        _view = message.PlayerViewOfGame;
    }

    private int _turn;

    private async void ItsMyTurn(ItsYourTurnMessage message)
    {
        var turn = _turn++;
        try
        {
            var cards = message.PlayerViewOfGame.Cards;
            
            _logger.LogInformation("It's my turn. Top: {top} ({suit}). I have: {cards} ({turn})",
                message.PlayerViewOfGame.TopOfPile,
                message.PlayerViewOfGame.CurrentSuit.Display(),
                string.Join(", ", cards),
                turn);
            
            _view = message.PlayerViewOfGame;

            if (TryGetCard(out var card))
            {
                try
                {
                    _logger.LogInformation("Putting card: {card} ({turn})", card, turn);
                    var r = await _client.PutCardAsync(card);
                    _logger.LogInformation("Result: {result}", r.GetType().Name);
                }
                catch (Exception e)
                {
                    _logger.LogError("{message} ({turn})", e.Message, turn);
                    throw;
                }

                return;
            }

            for (var ii = 0; ii < 3; ii++)
            {
                _logger.LogTrace("ii:{ii}, ({turn})", ii, turn);
                card = await _client.DrawCardAsync();
                _logger.LogInformation("Drawing card: {card} ({turn})", card, turn);
                _view.Cards.Add(card);
                if (TryGetCard(out card))
                {
                    try
                    {
                        _logger.LogInformation("Putting card: {card} ({turn})", card, turn);
                        var r = await _client.PutCardAsync(card);
                        _logger.LogInformation("Result: {result}", r.GetType().Name);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("{message} ({turn})", e.Message, turn);
                        throw;
                    }

                    return;
                }
            }

            _logger.LogInformation("Passing ({turn})", turn);
            await _client.PassAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Argh ({turn})", turn);
            throw;
        }
        _logger.LogInformation("Done ({turn})", turn);
    }

    private bool TryGetCard(out Card card)
    {
        _logger.LogTrace("Try get card");
        card = default;
        foreach (var c in _view.Cards.Where(c => c.Rank == _view.TopOfPile.Rank || c.Suit == _view.CurrentSuit))
        {
            card = c;
            return true;
        }
        _logger.LogTrace("Try get card: no");
        return false;
    }

    private void PlayerPutEight(PlayerPutEightMessage message)
    {
        _logger.LogTrace("{playerId} put eight {card}", message.PlayerId, message.Card);
    }

    private void PlayerPutCard(PlayerPutCardMessage message)
    {
        _logger.LogTrace("{playerId} put {card}", message.PlayerId, message.Card);
    }

    private void PlayerDrewCard(PlayerDrewCardMessage message)
    {
        _logger.LogTrace("Player drew card: {playerId}", message.PlayerId);
    }

    private void PlayerPassed(PlayerPassedMessage message)
    {
        _logger.LogTrace("Player passed: {playerId}", message.PlayerId);
    }

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_gameEnded)
        {
            await Task.Delay(500, cancellationToken);
        }
    }
}