using System.Diagnostics;
using Deckster.Client.Communication;
using Deckster.Client.Games.Common;
using Deckster.Client.Logging;
using Deckster.Client.Protocol;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Games.CrazyEights;

[DebuggerDisplay("CrazyEightsClient {PlayerData}")]
public class CrazyEightsClient : GameClient
{
    private readonly ILogger _logger;
    
    public event Action<PlayerPutCardNotification>? PlayerPutCard;
    public event Action<PlayerPutEightNotification>? PlayerPutEight;
    public event Action<PlayerDrewCardNotification>? PlayerDrewCard;
    public event Action<PlayerPassedNotification>? PlayerPassed;
    public event Action<ItsYourTurnNotification>? ItsYourTurn;
    public event Action<GameStartedNotification>? GameStarted;
    public event Action<GameEndedNotification>? GameEnded;

    public PlayerData PlayerData => Channel.Player;

    public CrazyEightsClient(IClientChannel channel) : base(channel)
    {
        _logger = Log.Factory.CreateLogger(channel.Player.Name);
    }

    public Task<PlayerViewOfGame> PutCardAsync(Card card, CancellationToken cancellationToken = default)
    {
        var request = new PutCardRequest
        {
            Card = card
        };
        return SendAsync<PlayerViewOfGame>(request, cancellationToken);
    }

    public Task<PlayerViewOfGame> PutEightAsync(Card card, Suit newSuit, CancellationToken cancellationToken = default)
    {
        var request = new PutEightRequest
        {
            Card = card,
            NewSuit = newSuit
        };
        return SendAsync<PlayerViewOfGame>(request, cancellationToken);
    }

    public async Task<Card> DrawCardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Draw card");
        var result = await GetAsync<CardResponse>(new DrawCardRequest(), cancellationToken);
        _logger.LogTrace("Draw card: {result}", result.Card);
        return result.Card;
    }

    public Task<EmptyResponse> PassAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<EmptyResponse>(new PassRequest(), cancellationToken);
    }

    protected override async void OnNotification(DecksterNotification notification)
    {
        try
        {
            switch (notification)
            {
                case GameStartedNotification m:
                    GameStarted?.Invoke(m);
                    break;
                case GameEndedNotification m:
                    GameEnded?.Invoke(m);
                    break;
                case PlayerPutCardNotification m:
                    PlayerPutCard?.Invoke(m);
                    break;
                case PlayerPutEightNotification m: 
                    PlayerPutEight?.Invoke(m);
                    break;
                case PlayerDrewCardNotification m: 
                    PlayerDrewCard?.Invoke(m);
                    break;
                case PlayerPassedNotification m:
                    PlayerPassed?.Invoke(m);
                    break;
                case ItsYourTurnNotification m:
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