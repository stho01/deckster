using Deckster.Client.Games.Yaniv;
using Deckster.Core.Games.Yaniv;
using Microsoft.Extensions.Logging;

namespace Deckster.Yaniv.SampleClient;

public class YanivPoorAi
{
    private readonly TaskCompletionSource _tcs = new();
    private PlayerViewOfGame _view = new();
    private readonly YanivClient _client;
    private readonly ILogger _logger;

    public YanivPoorAi(YanivClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
        client.RoundStarted += RoundStarted;
        client.RoundEnded += RoundEnded;
        client.ItsYourTurn += ItsMyTurn;
        client.GameEnded += GameEnded;
        client.PlayerPutCards += PlayerPutCards;
        client.Disconnected += Disconnected;
    }

    private void Disconnected(string n)
    {
        _logger.LogInformation("Disconnected: {reason}", n);
    }

    private void PlayerPutCards(PlayerPutCardsNotification n)
    {
        var player = _view.OtherPlayers.FirstOrDefault(p => p.Id == n.PlayerId);
        if (player == null)
        {
            return;
        }
        _logger.LogInformation($"{player.Name} put cards: {{cards}}", string.Join(", ", n.Cards));
    }

    private void RoundEnded(RoundEndedNotification n)
    {
        var winner = _view.OtherPlayers.FirstOrDefault(p => p.Id == n.WinnerPlayerId);
        if (winner != null)
        {
            _logger.LogInformation("Round ended. Winner: {winner}", winner.Name);    
        }
        else
        {
            _logger.LogInformation("Round ended. I won!");    
        }
    }

    private void GameEnded(GameEndedNotification n)
    {
        _tcs.TrySetResult();
    }

    private async void ItsMyTurn(ItsYourTurnNotification obj)
    {
        var view = _view;

        if (view.CardsOnHand.SumYanivPoints() <= 5)
        {
            await _client.CallYanivAsync();
            return;
        }
        
        var cards = view.CardsOnHand.GetCardsToPlay();
        var drawn = await _client.PutCardsOrThrowAsync(cards, DrawCardFrom.StockPile);
        view.CardsOnHand.Add(drawn);
    }

    private void RoundStarted(RoundStartedNotification n)
    {
        _view = n.PlayerViewOfGame;
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(_tcs.SetCanceled);
        return _tcs.Task;
    }
}