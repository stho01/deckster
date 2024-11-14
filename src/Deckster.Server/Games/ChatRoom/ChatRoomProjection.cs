using Deckster.Core.Games.ChatRoom;
using Deckster.Games.ChatRoom;

namespace Deckster.Server.Games.ChatRoom;

public class ChatRoomProjection : GameProjection<Deckster.Games.ChatRoom.ChatRoom>
{
    public override (Deckster.Games.ChatRoom.ChatRoom game, object startEvent) Create(IGameHost host)
    {
        var started = new ChatCreatedEvent();

        var chat = Deckster.Games.ChatRoom.ChatRoom.Instantiate(started);
        chat.Name = host.Name;
        chat.RespondAsync = host.RespondAsync;
        chat.PlayerSaid += host.NotifyAllAsync;
        
        return (chat, started);
    }

    public Task Apply(SendChatRequest @event, Deckster.Games.ChatRoom.ChatRoom chatRoom) => chatRoom.ChatAsync(@event);
}