using System.Text.Json;
using Deckster.Client.Common;

namespace Deckster.Server.Communication;

public interface IServerChannel : IDisposable
{
    event Action<IServerChannel> Disconnected;
    
    PlayerData Player { get; }
    ValueTask ReplyAsync<TResponse>(TResponse response, JsonSerializerOptions options, CancellationToken cancellationToken = default);
    ValueTask PostMessageAsync<TNotification>(TNotification notification, JsonSerializerOptions options, CancellationToken cancellationToken = default);
    Task WeAreDoneHereAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    
    void Start<TRequest>(Action<PlayerData, TRequest> handle, JsonSerializerOptions options, CancellationToken cancellationToken);
}