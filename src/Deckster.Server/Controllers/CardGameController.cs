using System.Net.WebSockets;
using Deckster.Server.Authentication;
using Deckster.Server.Games;
using Deckster.Server.Games.Common.Meta;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

// Marker interface for discoverability
public interface ICardGameController;

public abstract class CardGameController<TGameHost> : Controller, ICardGameController
    where TGameHost : IGameHost, new()
{
    protected readonly GameHostRegistry HostRegistry;

    protected CardGameController(GameHostRegistry hostRegistry)
    {
        HostRegistry = hostRegistry;
    }

    [HttpGet("metadata")]
    public object GetMetadata()
    {
        return GameMeta.For(typeof(TGameHost));
    }
    
    [HttpGet("")]
    public ViewResult Overview()
    {
        var games = HostRegistry.GetHosts<TGameHost>().Select(h => new GameVm
        {
            Id = h.Name,
            Players = h.GetPlayers()
        });
        return View(games);
    }
    
    [HttpGet("games")]
    public object Games()
    {
        var games = HostRegistry.GetHosts<TGameHost>().Select(h => new GameVm
        {
            Id = h.Name,
            Players = h.GetPlayers()
        });
        return games;
    }
    
    [HttpGet("games/{id}")]
    public object GameState(string id)
    {
        if (!HostRegistry.TryGet<TGameHost>(id, out var host))
        {
            return StatusCode(404, new ResponseMessage("Game not found: '{id}'"));
        }
        var vm = new GameVm
        {
            Id = host.Name,
            Players = host.GetPlayers()
        };

        return Request.AcceptsJson() ? vm : View(vm);
    }
    
    [HttpPost("create/{name}")]
    public object Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return StatusCode(400, new ResponseMessage("Name is required"));
        }
        
        var host = new TGameHost
        {
            Name = name
        };
        HostRegistry.Add(host);
        return StatusCode(200, new {Id = host.Name });
    }
    
    [HttpPost("games/{id}/start")]
    public async Task<object> Start(string id)
    {
        if (!HostRegistry.TryGet<TGameHost>(id, out var host))
        {
            return StatusCode(404, new ResponseMessage("Game not found: '{id}'"));
        }
        
        await host.Start();
        return StatusCode(200, new ResponseMessage("Game '{id}' started"));
    }
    
    [HttpGet("join/{gameName}")]
    public async Task Join(string gameName)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Not WS request"));
            return;
        }

        if (!HttpContext.TryGetUser(out var decksterUser))
        {
            HttpContext.Response.StatusCode = 401;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Unauthorized"));
            return;
        }
        using var actionSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        
        if (!await HostRegistry.StartJoinAsync<TGameHost>(decksterUser, actionSocket, gameName))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Could not connect"));
            await actionSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "Could not connect", default);
        }
    }

    [HttpGet("join/{connectionId:guid}/finish")]
    public async Task FinishJoin(Guid connectionId)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Not WS request"));
            return;
        }
        
        using var eventSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        if (!await HostRegistry.FinishJoinAsync(connectionId, eventSocket))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Could not connect"));
        }
    }
}