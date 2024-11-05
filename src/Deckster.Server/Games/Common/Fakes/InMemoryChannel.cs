using System.Text.Json;
using Deckster.Core.Communication;
using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;
using Deckster.Server.Communication;

namespace Deckster.Server.Games.Common.Fakes;

public partial class InMemoryChannel : IClientChannel
{
    public event Action<string>? OnDisconnected;
    private readonly AsyncMessageQueue<byte[]> _requests = new();
    private readonly AsyncMessageQueue<byte[]> _responses = new();
    private Task _readNotificationsTask;
    private readonly CancellationTokenSource _cts = new();
    
    public async Task<TResponse> SendAsync<TResponse>(object request, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        _requests.Add(JsonSerializer.SerializeToUtf8Bytes(request, options));
        var bytes = await _responses.ReadAsync(cancellationToken);
        var response = JsonSerializer.Deserialize<TResponse>(bytes, options);
        if (response == null)
        {
            throw new Exception("OMG GOT NULLZ RESPOANZ!");
        }

        return response;
    }

    public void StartReadNotifications<TNotification>(Action<TNotification> handle, JsonSerializerOptions options)
    {
        _readNotificationsTask = ReadNotificationsAsync(handle, options);
    }

    private async Task ReadNotificationsAsync<TNotification>(Action<TNotification> handle, JsonSerializerOptions options)
    {
        while (!_cts.IsCancellationRequested)
        {
            var bytes = await _notifications.ReadAsync();
            var notification = JsonSerializer.Deserialize<TNotification>(bytes, options);
            if (notification == null)
            {
                throw new Exception("OMG GOT NULLZ NOETFIKEYSHON!");
            }   
            handle(notification);
        }
    }
    
    public ValueTask DisposeAsync()
    {
        _cts.Dispose();
        return ValueTask.CompletedTask;
    }

    public override string ToString()
    {
        return $"{GetType().Name} {Player}";
    }
}

public partial class InMemoryChannel : IServerChannel
{
    public event Action<IServerChannel>? Disconnected;
    public PlayerData Player { get; init; }

    private Task _readRequestsTask;
    private readonly AsyncMessageQueue<byte[]> _notifications = new();

    public void StartReading<TRequest>(Action<IServerChannel, TRequest> handle, JsonSerializerOptions options, CancellationToken cancellationToken) where TRequest : DecksterRequest
    {
        _readRequestsTask = ReadRequestsAsync(handle, options, cancellationToken);
    }

    private async Task ReadRequestsAsync<TRequest>(Action<IServerChannel, TRequest> handle, JsonSerializerOptions options, CancellationToken cancellationToken)  where TRequest : DecksterRequest
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var bytes = await _requests.ReadAsync(cancellationToken);
            var request = JsonSerializer.Deserialize<TRequest>(bytes, options);
            if (request == null)
            {
                throw new Exception("OMG GOT NULLZ REKWEST!");
            }

            request.PlayerId = Player.Id;

            handle(this, request);
        }
    }
    
    public ValueTask ReplyAsync<TResponse>(TResponse response, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        _responses.Add(JsonSerializer.SerializeToUtf8Bytes(response, options));
        return ValueTask.CompletedTask;
    }

    public ValueTask SendNotificationAsync<TNotification>(TNotification notification, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        _notifications.Add(JsonSerializer.SerializeToUtf8Bytes(notification, options));
        return ValueTask.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        OnDisconnected?.Invoke("hest");
        Disconnected?.Invoke(this);
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        
    }
}