using System.Net.WebSockets;
using System.Text;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Communication.WebSockets;
using Deckster.Client.Protocol;

namespace Deckster.Server.Communication;

public class WebSocketServerChannel : IServerChannel
{
    public event Action<PlayerData, DecksterRequest>? Received;
    public event Action<IServerChannel>? Disconnected;

    public PlayerData Player { get; }
    private readonly WebSocket _requestSocket;
    private readonly WebSocket _messageSocket;
    private readonly TaskCompletionSource _taskCompletionSource;

    private Task? _listenTask;
    
    public WebSocketServerChannel(PlayerData player, WebSocket requestSocket, WebSocket messageSocket, TaskCompletionSource taskCompletionSource)
    {
        Player = player;
        _requestSocket = requestSocket;
        _messageSocket = messageSocket;
        _taskCompletionSource = taskCompletionSource;
    }

    public void Start(CancellationToken cancellationToken)
    {
        _listenTask = ListenAsync(cancellationToken);
    }
    
    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var buffer = new byte[4096];
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _requestSocket.ReceiveAsync(buffer, cancellationToken);
                Console.WriteLine($"Got messageType: '{result.MessageType}'");

                switch (result.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await DisconnectAsync(true, "Client disconnected");
                        return;
                }
            
                var request = DecksterJson.Deserialize<DecksterRequest>(new ArraySegment<byte>(buffer, 0, result.Count));
                if (request == null)
                {
                    Console.WriteLine("Command is null.");
                    Console.WriteLine($"Raw: {Encoding.UTF8.GetString(new ArraySegment<byte>(buffer, 0, result.Count))}");
                    await _requestSocket.SendMessageAsync(new FailureResponse("Command is null"), cancellationToken: cancellationToken);
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
    
    public ValueTask ReplyAsync(DecksterResponse response, CancellationToken cancellationToken = default)
    {
        var bytes = DecksterJson.SerializeToBytes(response);
        return _requestSocket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }

    public ValueTask PostMessageAsync(DecksterMessage message, CancellationToken cancellationToken = default)
    {
        
        var bytes = DecksterJson.SerializeToBytes(message);
        Console.WriteLine($"Post {bytes.Length} bytes to {Player.Name}");
        return _messageSocket.SendAsync(bytes, WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }
    
    public Task WeAreDoneHereAsync(CancellationToken cancellationToken = default)
    {
        return _requestSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
    }

    public async Task DisconnectAsync(bool normal, string reason)
    {
        var status = normal ? WebSocketCloseStatus.NormalClosure : WebSocketCloseStatus.ProtocolError;
        _taskCompletionSource.SetResult();
        await _requestSocket.CloseOutputAsync(status, reason, default);
        await _messageSocket.CloseOutputAsync(status, reason, default);
        Disconnected?.Invoke(this);
    }

    public void Dispose()
    {
        _requestSocket.Dispose();
        _messageSocket.Dispose();
        _taskCompletionSource.TrySetResult();
    }
}