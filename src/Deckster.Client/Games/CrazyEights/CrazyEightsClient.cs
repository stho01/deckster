using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Games.Common;
using Deckster.Client.Logging;
using Deckster.Client.Protocol;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Games.CrazyEights;

public class CrazyEightsClient : GameClient
{
    private readonly ILogger _logger;
    
    public event Action<PlayerPutCardMessage>? PlayerPutCard;
    public event Action<PlayerPutEightMessage>? PlayerPutEight;
    public event Action<PlayerDrewCardMessage>? PlayerDrewCard;
    public event Action<PlayerPassedMessage>? PlayerPassed;
    public event Action<ItsYourTurnMessage>? ItsYourTurn;
    public event Action<GameStartedMessage>? GameStarted;
    public event Action<GameEndedMessage>? GameEnded;

    public PlayerData PlayerData => _channel.PlayerData;

    public CrazyEightsClient(IClientChannel channel) : base(channel)
    {
        _logger = Log.Factory.CreateLogger(channel.PlayerData.Name);
        channel.OnMessage += HandleMessageAsync;
    }

    public Task<DecksterResponse> PutCardAsync(Card card, CancellationToken cancellationToken = default)
    {
        var command = new PutCardRequest
        {
            Card = card
        };
        return _channel.SendAsync(command, cancellationToken);
    }

    public Task<DecksterResponse> PutEightAsync(Card card, Suit newSuit, CancellationToken cancellationToken = default)
    {
        var command = new PutEightRequest
        {
            Card = card,
            NewSuit = newSuit
        };
        return _channel.SendAsync(command, cancellationToken);
    }

    public async Task<Card> DrawCardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Draw card");
        var result = await _channel.GetAsync<CardResponse>(new DrawCardRequest(), cancellationToken);
        _logger.LogTrace("Draw card: {result}", result.Card);
        return result.Card;
    }

    public Task<DecksterResponse> PassAsync(CancellationToken cancellationToken = default)
    {
        return _channel.SendAsync(new PassRequest(), cancellationToken);
    }

    private async void HandleMessageAsync(IClientChannel channel, DecksterMessage message)
    {
        try
        {
            switch (message)
            {
                case GameStartedMessage m:
                    GameStarted?.Invoke(m);
                    break;
                case GameEndedMessage m:
                    await _channel.DisconnectAsync();
                    GameEnded?.Invoke(m);
                    break;
                case PlayerPutCardMessage m:
                    PlayerPutCard?.Invoke(m);
                    break;
                case PlayerPutEightMessage m: 
                    PlayerPutEight?.Invoke(m);
                    break;
                case PlayerDrewCardMessage m: 
                    PlayerDrewCard?.Invoke(m);
                    break;
                case PlayerPassedMessage m:
                    PlayerPassed?.Invoke(m);
                    break;
                case ItsYourTurnMessage m:
                    ItsYourTurn?.Invoke(m);
                    break;
                default:
                    return;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}