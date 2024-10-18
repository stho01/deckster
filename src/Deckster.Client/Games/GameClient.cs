using System.Text.Json;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;

namespace Deckster.Client.Games;

public interface IGameClient : IDisposable, IAsyncDisposable;

public abstract class GameClient<TRequest, TResponse, TNotification> : IGameClient 
    where TRequest : DecksterRequest
    where TResponse : DecksterResponse
    where TNotification : DecksterNotification
{
    protected readonly IClientChannel Channel;
    public event Action<string>? Disconnected;

    private static readonly JsonSerializerOptions JsonOptions = DecksterJson.Create(o =>
    {
        o.AddAll<TRequest>().AddAll<TResponse>().AddAll<TNotification>();
    });

    protected GameClient(IClientChannel channel)
    {
        Channel = channel;
        channel.OnDisconnected += reason => Disconnected?.Invoke(reason);
        channel.StartReadNotifications<TNotification>(OnNotification, JsonOptions);
    }

    protected abstract void OnNotification(TNotification notification);

    protected async Task<TResponse> SendAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var response = await Channel.SendAsync<DecksterResponse>(request, JsonOptions, cancellationToken);
        return response switch
        {
            TResponse expected => expected,
            FailureResponse f => throw new Exception(f.Message),
            null => throw new Exception("Result is null. Wat"),
            _ => throw new Exception($"Unknown result '{response.GetType().Name}'")
        };
    }

    public async Task DisconnectAsync()
    {
        await Channel.DisconnectAsync();
    }
    
    protected async Task<TWanted> GetAsync<TWanted>(TRequest request, CancellationToken cancellationToken = default) where TWanted : TResponse
    {
        var response = await SendAsync(request, cancellationToken);
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