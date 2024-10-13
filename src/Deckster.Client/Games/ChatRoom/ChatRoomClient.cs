using Deckster.Client.Communication;

namespace Deckster.Client.Games.ChatRoom;

public class ChatRoomClient : GameClient<ChatRequest, ChatResponse, ChatNotification>
{
    public event Action<ChatNotification>? OnMessage;
    public event Action<string>? OnDisconnected;

    public ChatRoomClient(IClientChannel channel) : base(channel)
    {
        channel.OnDisconnected += s => OnDisconnected(s);
    }

    protected override void OnNotification(ChatNotification notification)
    {
        OnMessage?.Invoke(notification);
    }

    public Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        return base.SendAsync(request, cancellationToken);
    }
}