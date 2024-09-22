using Deckster.Client.Communication;
using Deckster.Client.Communication.WebSockets;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.ChatRoom;

public class ChatRoomClient : GameClient
{
    public event Action<DecksterNotification>? OnMessage;
    public event Action<string>? OnDisconnected;

    public ChatRoomClient(WebSocketClientChannel channel) : base(channel)
    {
        channel.OnMessage += MessageReceived;
        channel.OnDisconnected += (channel, s) => OnDisconnected(s);
    }

    private void MessageReceived(IClientChannel channel, DecksterNotification notification)
    {
        OnMessage?.Invoke(notification);
    }

    public Task<DecksterResponse> SendAsync(DecksterRequest message, CancellationToken cancellationToken = default)
    {
        return Channel.SendAsync(message, cancellationToken);
    }
}