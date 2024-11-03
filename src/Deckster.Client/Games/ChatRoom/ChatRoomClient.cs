using Deckster.Client.Communication;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.ChatRoom;

public class ChatRoomClient : GameClient
{
    public event Action<ChatNotification>? OnMessage;
    public event Action<string>? OnDisconnected;

    public ChatRoomClient(IClientChannel channel) : base(channel)
    {
        channel.OnDisconnected += OnDisconnected;
    }

    protected override void OnNotification(DecksterNotification notification)
    {
        switch (notification)
        {
            case ChatNotification chat:
                OnMessage?.Invoke(chat);
                break;
        }
    }

    public Task<ChatResponse> ChatAsync(SendChatRequest request, CancellationToken cancellationToken = default)
    {
        return SendAsync<ChatResponse>(request, cancellationToken);
    }
}