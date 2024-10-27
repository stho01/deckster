using Deckster.Client.Games.ChatRoom;

namespace Deckster.Server.Games.ChatRoom;

public class ChatProjection : GameProjection<Chat>
{
    public override (Chat game, object startEvent) Create(IGameHost host)
    {
        var started = new ChatCreatedEvent();

        return (Chat.Create(started), started);
    }

    public Task Apply(SendChatRequest @event, Chat chat) => chat.ChatAsync(@event);
}