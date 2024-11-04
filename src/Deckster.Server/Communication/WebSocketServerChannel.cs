using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Deckster.Client.Communication.WebSockets;
using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;
using Deckster.Core.Serialization;

namespace Deckster.Server.Communication;

public class WebSocketServerChannel : IServerChannel
{
    public event Action<IServerChannel>? Disconnected;

    public PlayerData Player { get; }
    private readonly WebSocket _actionSocket;
    private readonly WebSocket _notificationSocket;
    private readonly TaskCompletionSource _tcs;
    private readonly ILogger<WebSocketServerChannel> _logger;

    private Task? _listenTask;
    
    public WebSocketServerChannel(PlayerData player,
        WebSocket actionSocket,
        WebSocket notificationSocket,
        TaskCompletionSource tcs,
        ILogger<WebSocketServerChannel> logger)
    {
        Player = player;
        _actionSocket = actionSocket;
        _notificationSocket = notificationSocket;
        _tcs = tcs;
        _logger = logger;
    }

    public void StartReading<TRequest>(Action<IServerChannel, TRequest> handle, JsonSerializerOptions options, CancellationToken cancellationToken) where TRequest : DecksterRequest
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
                        _logger.LogInformation("Got close message: {reason}", result.CloseStatusDescription);
                        switch (result.CloseStatusDescription)
                        {
                            case ClosingReasons.ClientDisconnected:
                                await CloseNotificationSocketAsync(result.CloseStatusDescription);
                                if (_actionSocket.State == WebSocketState.CloseReceived)
                                {
                                    _logger.LogInformation("Sending close ack for action socket");
                                    await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);    
                                }
                                Disconnected?.Invoke(this);
                                _tcs.SetResult();
                                return;
                            case ClosingReasons.ServerDisconnected:
                                if (_actionSocket.State == WebSocketState.CloseReceived)
                                {
                                    _logger.LogInformation("Sending close ack for action socket");
                                    await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);    
                                }
                                return;
                        }
                        
                        return;
                }
            
                var request = JsonSerializer.Deserialize<TRequest>(new ArraySegment<byte>(buffer, 0, result.Count), options);
                
                if (request == null)
                {
                    Console.WriteLine("Command is null.");
                    Console.WriteLine($"Raw: {Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count))}");
                    await _actionSocket.SendMessageAsync(new EmptyResponse("Command is null"), options, cancellationToken);
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

    private async Task CloseNotificationSocketAsync(string reason)
    {
        if (_notificationSocket.State == WebSocketState.Open)
        {
            try
            {
                await _notificationSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, default);
            }
            catch (WebSocketException e)
            {
                _logger.LogError(e, "Error closing notification socket");
            }
        }
    }

    private async Task WaitForActionSocketToCloseAsync()
    {
        var timeout = Task.Delay(10000);

        if (await Task.WhenAny(timeout, WaitAsync()) == timeout)
        {
            _actionSocket.Dispose();
        }
        
        async Task WaitAsync()
        {
            while (_actionSocket.State < WebSocketState.Closed)
            {
                await Task.Delay(10);
            }
        } 
    }

    public async Task DisconnectAsync()
    {
        await CloseNotificationSocketAsync(ClosingReasons.ServerDisconnected);
        await WaitForActionSocketToCloseAsync();
        
        Disconnected?.Invoke(this);
        _tcs.TrySetResult();
    }

    public void Dispose()
    {
        _actionSocket.Dispose();
        _notificationSocket.Dispose();
        _tcs.TrySetResult();
    }
}