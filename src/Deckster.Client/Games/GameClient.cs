using Deckster.Client.Communication;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;

namespace Deckster.Client.Games;

public interface IGameClient : IDisposable, IAsyncDisposable;

public abstract class GameClient : IGameClient
{
    protected readonly IClientChannel Channel;
    public event Action<string>? Disconnected;

    protected GameClient(IClientChannel channel)
    {
        Channel = channel;
        channel.OnDisconnected += reason => Disconnected?.Invoke(reason);
        channel.StartReadNotifications<DecksterNotification>(OnNotification, DecksterJson.Options);
    }

    protected abstract void OnNotification(DecksterNotification notification);

    protected async Task<TResponse> SendAsync<TResponse>(DecksterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Channel.SendAsync<DecksterResponse>(request, DecksterJson.Options, cancellationToken);
        return response switch
        {
            null => throw new Exception("OMG RESPAWNS IZ NULLZ"),
            { HasError: true } => throw new Exception(response.Error), 
            TResponse expected => expected,
            _ => throw new Exception($"Unknown result '{response.GetType().Name}'")
        };
    }

    public async Task DisconnectAsync()
    {
        await Channel.DisconnectAsync();
    }
    
    protected async Task<TWanted> GetAsync<TWanted>(DecksterRequest request, CancellationToken cancellationToken = default) where TWanted : DecksterResponse
    {
        var response = await SendAsync<TWanted>(request, cancellationToken);
        return response switch
        {
            TWanted r => r,
            _ => throw new Exception($"Unexpected response '{response.GetType().Name}'")
        };
    }
    
    public void Dispose()
    {
        Channel.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
    }
}