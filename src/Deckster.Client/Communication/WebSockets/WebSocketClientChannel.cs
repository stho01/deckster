using System.Net.WebSockets;
using System.Text.Json;
using Deckster.Client.Common;
using Deckster.Client.Communication.Handshake;
using Deckster.Client.Logging;
using Deckster.Client.Serialization;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Communication.WebSockets;

public class WebSocketClientChannel : IClientChannel
{
    public PlayerData Player { get; }
    public event Action<string>? OnDisconnected;
    private readonly ClientWebSocket _actionSocket;
    private readonly ClientWebSocket _notificationSocket;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new(1,1);
    private readonly byte[] _actionBuffer = new byte[1024];

    private Task? _readTask;
    
    public bool IsConnected { get; private set; }

    public WebSocketClientChannel(ClientWebSocket actionSocket, ClientWebSocket notificationSocket, PlayerData playerData)
    {
        IsConnected = true;
        _logger =  Log.Factory.CreateLogger($"{nameof(WebSocketClientChannel)} {playerData.Name}");
        _actionSocket = actionSocket;
        Player = playerData;
        _notificationSocket = notificationSocket;
    }

    public async Task<TResponse> SendAsync<TResponse>(object request, JsonSerializerOptions options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            if (!IsConnected)
            {
                throw new Exception("Not connected");
            }
            await _actionSocket.SendMessageAsync(request, options, cancellationToken);
            var result = await _actionSocket.ReceiveAsync(_actionBuffer, cancellationToken);
        
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    var actionResult = DecksterJson.Deserialize<TResponse>(new ReadOnlySpan<byte>(_actionBuffer, 0, result.Count));
                    if (actionResult == null)
                    {
                        throw new Exception("OMG GOT NULLZ RESULTZ!");
                    }

                    return actionResult;
                case WebSocketMessageType.Close:
                    switch (result.CloseStatusDescription)
                    {
                        case ClosingReasons.ClientDisconnected:
                            if (_actionSocket.State == WebSocketState.CloseReceived)
                            {
                                await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);    
                            }
                            break;
                        default:
                            // Server disconnected
                            if (_actionSocket.State == WebSocketState.CloseReceived)
                            {
                                await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, ClosingReasons.ServerDisconnected, default);    
                            }
                            // await DoDisconnectAsync(WebSocketCloseStatus.NormalClosure, ClosingReasons.ServerDisconnected);
                            break;
                    }
                    
                    
                    throw new Exception($"WebSocket disconnected: {result.CloseStatus} '{result.CloseStatusDescription}'");
                default:
                    throw new Exception($"Unsupported message type: '{result.MessageType}'");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task DisconnectAsync()
    {
        _logger.LogDebug("Starting disconnect");
        if (_actionSocket.State == WebSocketState.Open)
        {
            _logger.LogDebug("Sending close request for action socket");
            await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, ClosingReasons.ClientDisconnected, default);    
        }

        var buffer = new byte[256];
        while (_actionSocket.State == WebSocketState.CloseSent)
        {
            _logger.LogDebug("Waiting for close ack for action socket");
            _ = await _actionSocket.ReceiveAsync(buffer, default);
        }
        _logger.LogDebug("Action socket closed");
        
        FireDisonnected(ClosingReasons.ClientDisconnected);
    }

    private void FireDisonnected(string reason)
    {
        var onDisconnected = OnDisconnected;
        if (onDisconnected == null)
        {
            return;
        }
        OnDisconnected = null;
        onDisconnected.Invoke(reason);
    }

    public void StartReadNotifications<TNotification>(Action<TNotification> handle, JsonSerializerOptions options)
    {
        _readTask = ReadNotifications(handle, options);
    }
    
    private async Task ReadNotifications<TNotification>(Action<TNotification> handle, JsonSerializerOptions options)
    {
        var buffer = new byte[4096];
        _cts.Token.Register(() => _actionSocket.Dispose());
        while (!_cts.Token.IsCancellationRequested)
        {
            var result = await _notificationSocket.ReceiveAsync(buffer, _cts.Token);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                {
                    var message = JsonSerializer.Deserialize<TNotification>(new ReadOnlySpan<byte>(buffer, 0, result.Count), options);
                    if (message != null)
                    {
                        handle(message);
                    }
                    break;
                }
                // https://mcguirev10.com/2019/08/17/how-to-close-websocket-correctly.html
                case WebSocketMessageType.Close:
                    _logger.LogInformation("Got close message: {reason}", result.CloseStatusDescription);
                    switch (result.CloseStatusDescription)
                    {
                        // Client initiated disconnect
                        case ClosingReasons.ClientDisconnected:
                            if (_notificationSocket.State == WebSocketState.CloseReceived)
                            {
                                _logger.LogDebug("Sending close ack for notification socket");
                                await _notificationSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, ClosingReasons.ClientDisconnected, default);    
                            }
                            return;
                        // Server disconnected
                        default:
                            if (_actionSocket.State == WebSocketState.Open)
                            {
                                _logger.LogDebug("Sending close request for action socket");
                                await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, ClosingReasons.ServerDisconnected, default);    
                            }

                            if (_actionSocket.State == WebSocketState.CloseSent)
                            {
                                _logger.LogDebug("Waiting for close ack for action socket");
                                _ = await _actionSocket.ReceiveAsync(buffer, default);
                            }

                            if (_notificationSocket.State == WebSocketState.CloseReceived)
                            {
                                _logger.LogDebug("Sending close ack for notification socket");
                                await _notificationSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, ClosingReasons.ServerDisconnected, default);    
                            }
                            FireDisonnected(ClosingReasons.ServerDisconnected);
                            return;
                    }
                    
                    return;
            }
        }
    }
    
    public void Dispose()
    {
        DisconnectAsync().Wait();
        _actionSocket.Dispose();
        _notificationSocket.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        await CastAndDispose(_actionSocket);
        await CastAndDispose(_notificationSocket);
        await CastAndDispose(_cts);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
            {
                await resourceAsyncDisposable.DisposeAsync();
            }
            else
            {
                resource.Dispose();
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = DecksterJson.Create(m =>
    {
        m.AddAll<ConnectMessage>();
    });
    
    public static async Task<WebSocketClientChannel> ConnectAsync(Uri uri, string gameName, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var joinUri = uri.ToWebSocketUri($"join/{gameName}");
            
            var actionSocket = new ClientWebSocket();
            actionSocket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            await actionSocket.ConnectAsync(joinUri, cancellationToken);
            var joinMessage = await actionSocket.ReceiveMessageAsync<ConnectMessage>(JsonOptions, cancellationToken);

            Console.WriteLine($"Got join message: {joinMessage.Pretty()}");
            switch (joinMessage)
            {
                case ConnectFailureMessage failure:
                    
                    await actionSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "", default);
                    actionSocket.Dispose();
                    throw new Exception($"Join failed: {failure.ErrorMessage}");
                case HelloSuccessMessage hello:
                    var notificationSocket = new ClientWebSocket();
                    notificationSocket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
                    await notificationSocket.ConnectAsync(uri.ToWebSocketUri($"join/{hello.ConnectionId}/finish"), cancellationToken);

                    var finishMessage = await notificationSocket.ReceiveMessageAsync<ConnectMessage>(JsonOptions, cancellationToken);
                    Console.WriteLine($"Got finish message: {finishMessage.Pretty()}");
                    switch (finishMessage)
                    {
                        case ConnectSuccessMessage:
                        {
                            Console.WriteLine("Success");
                            return new WebSocketClientChannel(actionSocket, notificationSocket, hello.Player);
                        }
                        case ConnectFailureMessage finishFailure:
                            await actionSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "", default);
                            actionSocket.Dispose();
                            await notificationSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "", default);
                            notificationSocket.Dispose();
                            throw new Exception($"Finish join failed: '{finishFailure.ErrorMessage}'");
                        default:
                            await actionSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "", default);
                            actionSocket.Dispose();
                            await notificationSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "", default);
                            notificationSocket.Dispose();
                            throw new Exception($"Could not connect. Don't understand message: '{joinMessage.Pretty()}'");        
                    }
                    
                default:
                    await actionSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "", default);
                    actionSocket.Dispose();
                    throw new Exception($"Could not connect. Don't understand message: '{joinMessage.Pretty()}'");
            }
        }
        catch (WebSocketException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}