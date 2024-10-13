using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Logging;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Games.Uno;

public class UnoClient : GameClient<UnoRequest, UnoResponse, UnoGameNotification>
{
    private readonly ILogger _logger;
    
    public event Action<PlayerPutCardNotification>? PlayerPutCard;
    public event Action<PlayerPutWildNotification>? PlayerPutWild;
    public event Action<PlayerDrewCardNotification>? PlayerDrewCard;
    public event Action<PlayerPassedNotification>? PlayerPassed;
    public event Action<ItsYourTurnNotification>? ItsYourTurn;
    public event Action<GameStartedNotification>? GameStarted;
    public event Action<RoundEndedMessage>? RoundEnded;
    public event Action<RoundStartedMessage>? RoundStarted;
    public event Action<GameEndedNotification>? GameEnded;

    public PlayerData PlayerData => Channel.PlayerData;

    public UnoClient(IClientChannel channel) : base(channel)
    {
        _logger = Log.Factory.CreateLogger(channel.PlayerData.Name);
    }

    public Task<UnoResponse> PutCardAsync(UnoCard card, CancellationToken cancellationToken = default)
    {
        var command = new PutCardRequest
        {
            Card = card
        };
        return SendAsync(command, cancellationToken);
    }

    public Task<UnoResponse> PutWildAsync(UnoCard card, UnoColor newColor, CancellationToken cancellationToken = default)
    {
        var command = new PutWildRequest
        {
            Card = card,
            NewColor = newColor
        };
        return SendAsync(command, cancellationToken);
    }

    public async Task<UnoCard> DrawCardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Draw card");
        var result = await GetAsync<UnoCardResponse>(new DrawCardRequest(), cancellationToken);
        _logger.LogTrace("Draw card: {result}", result.Card);
        return result.Card;
    }

    public Task<UnoResponse> PassAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(new PassRequest(), cancellationToken);
    }

    protected override async void OnNotification(UnoGameNotification notification)
    {
        try
        {
            switch (notification)
            {
                case GameStartedNotification m:
                    GameStarted?.Invoke(m);
                    break;
                case GameEndedNotification m:
                    await Channel.DisconnectAsync();
                    GameEnded?.Invoke(m);
                    break;
                case PlayerPutCardNotification m:
                    PlayerPutCard?.Invoke(m);
                    break;
                case PlayerPutWildNotification m: 
                    PlayerPutWild?.Invoke(m);
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

    public Task SignalReadiness(CancellationToken cancellationToken = default)
    {
        var command = new ReadyToPlayRequest()
        {
          
        };
        return SendAsync(command, cancellationToken);
    }
}

