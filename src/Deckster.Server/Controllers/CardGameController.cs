using System.Net.WebSockets;
using Deckster.Server.Authentication;
using Deckster.Server.Games;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

public abstract class CardGameController : Controller
{
    protected readonly GameRegistry Registry;

    protected CardGameController(GameRegistry registry)
    {
        Registry = registry;
    }
    
    [HttpGet("join/{gameId:guid}")]
    public async Task Join(Guid gameId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Not WS request"));
            return;
        }

        var decksterUser = HttpContext.GetRequiredUser();
        using var commandSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        if (!await Registry.StartJoinAsync(decksterUser, commandSocket, gameId))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Could not connect"));
            await commandSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "Could not connect", default);
        }
    }

    [HttpGet("finishjoin/{connectionId:guid}")]
    public async Task FinishJoin(Guid connectionId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Not WS request"));
            return;
        }
        
        using var eventSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        if (!await Registry.FinishJoinAsync(connectionId, eventSocket))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Could not connect"));
        }
    }
}