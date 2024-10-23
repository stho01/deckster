using System.Text.Json;
using Deckster.Client.Common;
using Deckster.Client.Protocol;

namespace Deckster.Server.Communication;

public interface IServerChannel : IDisposable
{
    event Action<IServerChannel> Disconnected;
    
    PlayerData Player { get; }
    ValueTask ReplyAsync<TResponse>(TResponse response, JsonSerializerOptions options, CancellationToken cancellationToken = default);
    ValueTask SendNotificationAsync<TNotification>(TNotification notification, JsonSerializerOptions options, CancellationToken cancellationToken = default);
    Task WeAreDoneHereAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    
    void Start<TRequest>(Action<IServerChannel, TRequest> handle, JsonSerializerOptions options, CancellationToken cancellationToken) where TRequest : DecksterRequest;
}