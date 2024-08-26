using System.Net.WebSockets;
using System.Text;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Communication.WebSockets;
using Deckster.Client.Protocol;

namespace Deckster.Server.Communication;

public class WebSocketServerChannel : IServerChannel
{
    public bool IsConnected { get; private set; }
    public event Action<PlayerData, DecksterRequest>? Received;
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

    public void Start(CancellationToken cancellationToken)
    {
        _listenTask = ListenAsync(cancellationToken);
    }
    
    public ValueTask ReplyAsync(DecksterResponse response, CancellationToken cancellationToken = default)
    {
        var bytes = DecksterJson.SerializeToBytes(response);
        return _actionSocket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }

    public ValueTask PostMessageAsync(DecksterMessage message, CancellationToken cancellationToken = default)
    {
        
        var bytes = DecksterJson.SerializeToBytes(message);
        Console.WriteLine($"Post {bytes.Length} bytes to {Player.Name}");
        return _notificationSocket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }
    
    public Task WeAreDoneHereAsync(CancellationToken cancellationToken = default)
    {
        return DisconnectAsync();
    }
    
    private async Task ListenAsync(CancellationToken cancellationToken)
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
            
                var request = DecksterJson.Deserialize<DecksterRequest>(new ArraySegment<byte>(buffer, 0, result.Count));
                if (request == null)
                {
                    Console.WriteLine("Command is null.");
                    Console.WriteLine($"Raw: {Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count))}");
                    await _actionSocket.SendMessageAsync(new FailureResponse("Command is null"), cancellationToken: cancellationToken);
                }
                else
                {
                    Console.WriteLine($"Got request: {request.Pretty()}");
                    Received?.Invoke(Player, request);
                }
            }
        }
        catch (TaskCanceledException e)
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