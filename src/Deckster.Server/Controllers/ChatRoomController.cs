using Deckster.Server.Games;
using Deckster.Server.Games.TestGame;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("chatroom")]
public class ChatRoomController : CardGameController<ChatRoomHost>
{
    public ChatRoomController(GameHostRegistry hostRegistry) : base(hostRegistry)
    {
    }
}