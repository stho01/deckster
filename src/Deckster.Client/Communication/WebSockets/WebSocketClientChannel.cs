using System.Net.WebSockets;
using Deckster.Client.Common;
using Deckster.Client.Logging;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Communication.WebSockets;

public static class ClosingReasons
{
    public const string ClientDisconnected = "Client disconnected";
    public const string ServerDisconnected = "Server disconnected";
}

public class WebSocketClientChannel : IClientChannel
{
    public PlayerData PlayerData { get; }
    public event Action<IClientChannel, DecksterNotification>? OnMessage;
    public event Action<IClientChannel, string>? OnDisconnected;
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
        PlayerData = playerData;
        _notificationSocket = notificationSocket;
        _readTask = ReadNotifications();
    }

    public async Task<DecksterResponse> SendAsync(DecksterRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            if (!IsConnected)
            {
                throw new Exception("Not connected");
            }
            await _actionSocket.SendMessageAsync(request, cancellationToken);
            var result = await _actionSocket.ReceiveAsync(_actionBuffer, cancellationToken);
        
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    var actionResult = DecksterJson.Deserialize<DecksterResponse>(new ReadOnlySpan<byte>(_actionBuffer, 0, result.Count));
                    if (actionResult == null)
                    {
                        throw new Exception("OMG GOT NULLZ RESULTZ!");
                    }

                    return actionResult;
                case WebSocketMessageType.Close:
                    await _actionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);
                    await DoDisconnectAsync(WebSocketCloseStatus.NormalClosure, "Server disconnected");
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
    
    public Task DisconnectAsync() => DoDisconnectAsync(WebSocketCloseStatus.NormalClosure, ClosingReasons.ClientDisconnected);

    private async Task DoDisconnectAsync(WebSocketCloseStatus status, string reason)
    {
        try
        {
            await _semaphore.WaitAsync(default(CancellationToken));
            if (!IsConnected)
            {
                return;
            }
            IsConnected = false;
            
            await _actionSocket.CloseOutputAsync(status, reason, default);
            var response = await _actionSocket.ReceiveAsync(_actionBuffer, default);
            while (response.MessageType != WebSocketMessageType.Close)
            {
                response = await _actionSocket.ReceiveAsync(_actionBuffer, default);
            }

            while (_notificationSocket.State != WebSocketState.Closed)
            {
                await Task.Delay(10);
            }
            
            OnDisconnected?.Invoke(this, reason);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public static async Task<WebSocketClientChannel> ConnectAsync(Uri uri, string gameName, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var joinUri = uri.ToWebSocket($"join/{gameName}");
            
            var actionSocket = new ClientWebSocket();
            actionSocket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            await actionSocket.ConnectAsync(joinUri, cancellationToken);
            var joinMessage = await actionSocket.ReceiveMessageAsync<ConnectMessage>(cancellationToken);

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
                    await notificationSocket.ConnectAsync(uri.ToWebSocket($"join/{hello.ConnectionId}/finish"), cancellationToken);

                    var finishMessage = await notificationSocket.ReceiveMessageAsync<ConnectMessage>(cancellationToken: cancellationToken);
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
    
    private async Task ReadNotifications()
    {
        var buffer = new byte[4096];
        _cts.Token.Register(() => _actionSocket.Dispose());
        while (!_cts.Token.IsCancellationRequested && _actionSocket.State == WebSocketState.Open)
        {
            var result = await _notificationSocket.ReceiveAsync(buffer, _cts.Token);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                {
                    var message = DecksterJson.Deserialize<DecksterNotification>(new ReadOnlySpan<byte>(buffer, 0, result.Count));
                    if (message != null)
                    {
                        OnMessage?.Invoke(this, message);    
                    }
                    break;
                }
                // https://mcguirev10.com/2019/08/17/how-to-close-websocket-correctly.html
                case WebSocketMessageType.Close:
                    switch (result.CloseStatusDescription)
                    {
                        // Client initiated disconnect
                        case ClosingReasons.ClientDisconnected:
                            await _notificationSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);
                            return;
                        case ClosingReasons.ServerDisconnected:
                            await _notificationSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, default);
                            await DoDisconnectAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription);
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
}