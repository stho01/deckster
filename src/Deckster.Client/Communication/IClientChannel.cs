using System.Text.Json;
using Deckster.Client.Common;

namespace Deckster.Client.Communication;

public interface IClientChannel : IDisposable, IAsyncDisposable
{
    PlayerData PlayerData { get; }
    event Action<string>? OnDisconnected;
    Task DisconnectAsync();
    Task<TResponse> SendAsync<TResponse>(object request, JsonSerializerOptions options, CancellationToken cancellationToken = default);
    void StartReadNotifications<TNotification>(Action<TNotification> handle, JsonSerializerOptions options);
}