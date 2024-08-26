using Deckster.Client.Communication;
using Deckster.Client.Communication.WebSockets;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.ChatRoom;

public class ChatRoomClient : IDisposable, IAsyncDisposable
{
    public event Action<DecksterMessage>? OnMessage;
    public event Action<string>? OnDisconnected;
    
    private readonly IClientChannel _channel;

    public ChatRoomClient(WebSocketClientChannel channel)
    {
        _channel = channel;
        channel.OnMessage += MessageReceived;
        channel.OnDisconnected += (channel, s) => OnDisconnected(s);
    }

    private void MessageReceived(IClientChannel channel, DecksterMessage message)
    {
        OnMessage?.Invoke(message);
    }

    public Task<DecksterResponse> SendAsync(DecksterRequest message, CancellationToken cancellationToken = default)
    {
        return _channel.SendAsync(message, cancellationToken);
    }

    public void Dispose()
    {
        _channel.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
    }
}