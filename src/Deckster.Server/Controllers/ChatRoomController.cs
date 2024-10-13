using Deckster.Client.Games.ChatRoom;
using Deckster.Server.Games;
using Deckster.Server.Games.ChatRoom;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("chatroom")]
public class ChatRoomController : CardGameController<ChatRoomClient, ChatRoomHost>
{
    public ChatRoomController(GameHostRegistry hostRegistry) : base(hostRegistry)
    {
    }
}