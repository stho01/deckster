using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Games.Common;
using Deckster.Client.Logging;
using Deckster.Client.Protocol;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Games.Uno;

public class UnoClient : GameClient
{
    private readonly ILogger _logger;
    
    public event Action<PlayerPutCardMessage>? PlayerPutCard;
    public event Action<PlayerPutWildMessage>? PlayerPutWild;
    public event Action<PlayerDrewCardMessage>? PlayerDrewCard;
    public event Action<PlayerPassedMessage>? PlayerPassed;
    public event Action<ItsYourTurnMessage>? ItsYourTurn;
    public event Action<GameStartedMessage>? GameStarted;
    public event Action<RoundEndedMessage>? RoundEnded;
    public event Action<RoundStartedMessage>? RoundStarted;
    public event Action<GameEndedMessage>? GameEnded;

    public PlayerData PlayerData => _channel.PlayerData;

    public UnoClient(IClientChannel channel) : base(channel)
    {
        _logger = Log.Factory.CreateLogger(channel.PlayerData.Name);
        channel.OnMessage += HandleMessageAsync;
    }

    public Task<DecksterResponse> PutCardAsync(UnoCard card, CancellationToken cancellationToken = default)
    {
        var command = new PutCardRequest
        {
            Card = card
        };
        return _channel.SendAsync(command, cancellationToken);
    }

    public Task<DecksterResponse> PutWildAsync(UnoCard card, UnoColor newColor, CancellationToken cancellationToken = default)
    {
        var command = new PutWildRequest()
        {
            Card = card,
            NewColor = newColor
        };
        return _channel.SendAsync(command, cancellationToken);
    }

    public async Task<UnoCard> DrawCardAsync(CancellationToken cancellationToken = default)
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
                case PlayerPutWildMessage m: 
                    PlayerPutWild?.Invoke(m);
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

    public Task SignalReadiness(CancellationToken cancellationToken = default)
    {
        var command = new ReadyToPlayRequest()
        {
          
        };
        return _channel.SendAsync(command, cancellationToken);
    }
}

