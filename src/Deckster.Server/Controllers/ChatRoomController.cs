using Deckster.Server.Games;
using Deckster.Server.Games.TestGame;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("chatroom")]
public class ChatRoomController : CardGameController
{
    public ChatRoomController(GameRegistry registry) : base(registry)
    {
    }

    [HttpPost("create")]
    public async Task<object> Create()
    {
        var host = new ChatRoomHost();
        Registry.Add(host);
        return StatusCode(200, new { host.Id });
    }
}