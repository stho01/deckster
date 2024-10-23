using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Deckster.Client.Common;
using Deckster.Client.Communication.WebSockets;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;

namespace Deckster.Server.Communication;

public class WebSocketServerChannel : IServerChannel
{
    public bool IsConnected { get; private set; }
    public event Action<IServerChannel>? Disconnected;

    public PlayerData Player { get; }
    private readonly WebSocket _actionSocket;
    private readonly WebSocket _notificationSocket;
    private readonly TaskCompletionSource _taskCompletionSource;

    private Task? _listenTask;
    
    public WebSocketServerChannel(PlayerData player, WebSocket actionSocket, WebSocket notificationSocket, TaskCompletionSource taskCompletionSource)
    {
        Player = player;
        _actionSocket = actionSocket;
        _notificationSocket = notificationSocket;
        _taskCompletionSource = taskCompletionSource;
    }

    public void Start<TRequest>(Action<IServerChannel, TRequest> handle, JsonSerializerOptions options, CancellationToken cancellationToken) where TRequest : DecksterRequest
    {
        _listenTask = ListenAsync(handle, options, cancellationToken);
    }
    
    public ValueTask ReplyAsync<TResponse>(TResponse response, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(response, options);
        return _actionSocket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }

    public ValueTask SendNotificationAsync<TNotification>(TNotification notification, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        
        var bytes = JsonSerializer.SerializeToUtf8Bytes(notification, options);
        Console.WriteLine($"Post {bytes.Length} bytes to {Player.Name}");
        return _notificationSocket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }
    
    public Task WeAreDoneHereAsync(CancellationToken cancellationToken = default)
    {
        return DisconnectAsync();
    }
    
    private async Task ListenAsync<TRequest>(Action<IServerChannel, TRequest> handle, JsonSerializerOptions options, CancellationToken cancellationToken) where TRequest : DecksterRequest
    {
        try
        {
            var buffer = new byte[4096];
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _actionSocket.ReceiveAsync(buffer, cancellationToken);
                Console.WriteLine($"Got messageType: '{result.MessageType}'");

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        switch (result.CloseStatusDescription)
                        {
                            case ClosingReasons.ClientDisconnected:
                                await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);
                                await DoDisconnectAsync(result.CloseStatusDescription);
                                return;
                            case ClosingReasons.ServerDisconnected:
                                await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);
                                return;
                        }
                        
                        return;
                }
            
                var request = JsonSerializer.Deserialize<TRequest>(new ArraySegment<byte>(buffer, 0, result.Count), options);
                
                if (request == null)
                {
                    Console.WriteLine("Command is null.");
                    Console.WriteLine($"Raw: {Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count))}");
                    await _actionSocket.SendMessageAsync(new FailureResponse("Command is null"), options, cancellationToken);
                }
                else
                {
                    request.PlayerId = Player.Id;
                    Console.WriteLine($"Got request: {request.Pretty()}");
                    handle(this, request);
                }
            }
        }
        catch (TaskCanceledException)
        {
            return;
        }
    }

    public Task DisconnectAsync() => DoDisconnectAsync(ClosingReasons.ServerDisconnected);

    private async Task DoDisconnectAsync(string reason)
    {
        await _notificationSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, default);
        while (_actionSocket.State != WebSocketState.Closed)
        {
            await Task.Delay(10);
        }
        
        
        Disconnected?.Invoke(this);
        _taskCompletionSource.SetResult();
    }

    public void Dispose()
    {
        _actionSocket.Dispose();
        _notificationSocket.Dispose();
        _taskCompletionSource.TrySetResult();
    }
}