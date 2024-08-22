using System.Net.WebSockets;
using System.Runtime.InteropServices;
using Deckster.Client.Common;
using Deckster.Client.Logging;
using Deckster.Client.Protocol;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Communication.WebSockets;

public class WebSocketClientChannel : IClientChannel
{
    public PlayerData PlayerData { get; }
    public event Action<IClientChannel, DecksterMessage>? OnMessage;
    public event Action<IClientChannel>? OnDisconnected;
    private readonly ClientWebSocket _requestSocket;
    private readonly ClientWebSocket _messageSocket;
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _semaphore = new(1,1);
    private readonly byte[] _commandBuffer = new byte[1024];

    private Task? _readTask;

    public WebSocketClientChannel(ClientWebSocket requestSocket, ClientWebSocket messageSocket, PlayerData playerData)
    {
        _logger =  Log.Factory.CreateLogger($"{nameof(WebSocketClientChannel)} {playerData.Name}");
        _requestSocket = requestSocket;
        PlayerData = playerData;
        _messageSocket = messageSocket;
        _readTask = ReadMessages();
    }

    private async Task ReadMessages()
    {
        var buffer = new byte[4096];
        _cts.Token.Register(() => _requestSocket.Dispose());
        while (!_cts.Token.IsCancellationRequested && _requestSocket.State == WebSocketState.Open)
        {
            var result = await _messageSocket.ReceiveAsync(buffer, _cts.Token);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                {
                    var message = DecksterJson.Deserialize<DecksterMessage>(new ReadOnlySpan<byte>(buffer, 0, result.Count));
                    if (message != null)
                    {
                        OnMessage?.Invoke(this, message);    
                    }
                    break;
                }
                case WebSocketMessageType.Close:
                    OnDisconnected?.Invoke(this);
                    await _requestSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", _cts.Token);
                    await _messageSocket.CloseOutputAsync(WebSocketCloseStatus.Empty, "", _cts.Token);
                    return;
                    break;
            }
        }
    }

    public async Task<DecksterResponse> SendAsync(DecksterRequest message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _semaphore.WaitAsync(cancellationToken);
            await _requestSocket.SendMessageAsync(message, cancellationToken);
            var result = await _requestSocket.ReceiveAsync(_commandBuffer, cancellationToken);
        
            switch (result.MessageType)
            {
                case WebSocketMessageType.Text:
                    var commandResult = DecksterJson.Deserialize<DecksterResponse>(new ReadOnlySpan<byte>(_commandBuffer, 0, result.Count));
                    if (commandResult == null)
                    {
                        throw new Exception("OMG GOT NULLZ RESULTZ!");
                    }

                    return commandResult;
                case WebSocketMessageType.Close:
                    await DisconnectAsync(true, "Server disconnected", cancellationToken);
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
    
    public async Task DisconnectAsync(bool normal, string reason, CancellationToken cancellationToken = default)
    {
        var status = normal ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.ProtocolError;
        await Task.WhenAll(
            _messageSocket.CloseOutputAsync(status, reason, cancellationToken),
            _requestSocket.CloseOutputAsync(status, reason, cancellationToken)
        );
        OnDisconnected?.Invoke(this);
    }

    public static async Task<WebSocketClientChannel> ConnectAsync(Uri uri, Guid gameId, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var joinUri = uri.ToWebSocket($"join/{gameId}");
            
            var commandSocket = new ClientWebSocket();
            commandSocket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            await commandSocket.ConnectAsync(joinUri, cancellationToken);
            var connectMessage = await commandSocket.ReceiveMessageAsync<ConnectMessage>(cancellationToken);
            
            var eventSocket = new ClientWebSocket();
            eventSocket.Options.SetRequestHeader("Authorization", $"Bearer {token}");
            await eventSocket.ConnectAsync(uri.ToWebSocket($"finishjoin/{connectMessage.ConnectionId}"), cancellationToken);
        
            return new WebSocketClientChannel(commandSocket, eventSocket, connectMessage.PlayerData);
        }
        catch (WebSocketException e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    public void Dispose()
    {
        DisconnectAsync(true, "Closing").Wait();
        _requestSocket.Dispose();
        _messageSocket.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(true, "Closing");
        await CastAndDispose(_requestSocket);
        await CastAndDispose(_messageSocket);
        await CastAndDispose(_cts);
        if (_readTask != null) await CastAndDispose(_readTask);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable)
                await resourceAsyncDisposable.DisposeAsync();
            else
                resource.Dispose();
        }
    }
}