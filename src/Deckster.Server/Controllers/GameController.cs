using System.Net.WebSockets;
using Deckster.Core;
using Deckster.Games;
using Deckster.Games.CodeGeneration.Meta;
using Deckster.Server.Authentication;
using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

// Marker interface for discoverability
public interface IGameController;

public abstract class GameController<TGameHost, TGame> : Controller, IGameController
    where TGameHost : IGameHost
    where TGame : GameObject
{
    protected readonly GameHostRegistry HostRegistry;
    protected readonly IRepo Repo;

    protected GameController(GameHostRegistry hostRegistry, IRepo repo)
    {
        HostRegistry = hostRegistry;
        Repo = repo;
    }

    [HttpGet("metadata")]
    public GameMeta Meta()
    {
        var meta = GameMeta.TryGetFor(typeof(TGame), out var m) ? m : null;
        return meta;
    }
    
    [HttpGet("")]
    public ViewResult Overview()
    {
        var games = HostRegistry.GetHosts<TGameHost>().Select(h => new GameVm
        {
            Name = h.Name,
            Players = h.GetPlayers()
        });
        return View(games);
    }
    
    [HttpGet("games")]
    public IEnumerable<GameVm> Games()
    {
        var games = HostRegistry.GetHosts<TGameHost>().Select(h => new GameVm
        {
            Name = h.Name,
            Players = h.GetPlayers()
        });
        return games;
    }
    
    [HttpGet("games/{name}")]
    [ProducesResponseType<GameVm>(200)]
    [ProducesResponseType<ResponseMessage>(404)]
    public object GameState(string name)
    {
        if (!HostRegistry.TryGet<TGameHost>(name, out var host))
        {
            return StatusCode(404, new ResponseMessage($"Game not found: '{name}'"));
        }
        
        var vm = new GameVm
        {
            Name = host.Name,
            Players = host.GetPlayers()
        };

        return Request.AcceptsJson() ? vm : View(vm);
    }
    
    [HttpDelete("games/{name}")]
    [ProducesResponseType<GameVm>(200)]
    [ProducesResponseType<ResponseMessage>(404)]
    public async Task<object> CancelGame(string name)
    {
        if (!HostRegistry.TryGet<TGameHost>(name, out var host))
        {
            return StatusCode(404, new ResponseMessage($"Game not found: '{name}'"));
        }

        await host.EndAsync();
        
        var vm = new GameVm
        {
            Name = host.Name,
            Players = host.GetPlayers()
        };

        return Request.AcceptsJson() ? vm : View(vm);
    }

    [HttpPost("games/{name}/bot")]
    public ResponseMessage AddBot(string name)
    {
        if (!HostRegistry.TryGet<TGameHost>(name, out var host))
        {
            Response.StatusCode = 404;
            return new ResponseMessage($"Game not found: '{name}'");
        }

        if (!host.TryAddBot(out var error))
        {
            Response.StatusCode = 400;
            return new ResponseMessage(error);
        }

        return new ResponseMessage("ok");
    }

    [HttpGet("previousgames")]
    public async Task<IEnumerable<TGame>> PreviousGames()
    {
        var games = await Repo.Query<TGame>().ToListAsync();

        return games;
    }
    
    [HttpGet("previousgames/{id:guid}")]
    public async Task<TGame?> PreviousGame(Guid id)
    {
        var game = await Repo.GetAsync<TGame>(id);
        if (game == null)
        {
            Response.StatusCode = 404;
            return null;
        }

        return game;
    }
    
    [HttpGet("previousgames/{id:guid}/{version:long}")]
    public async Task<TGame?> PreviousGames(Guid id, long version)
    {
        var game = await Repo.GetGameAsync<TGame>(id, version);
        if (game == null)
        {
            Response.StatusCode = 404;
            return null;
        }
        return game;
    }
    
    [HttpPost("create/{name}")]
    [RequireUser]
    [ProducesResponseType<GameInfo>(200)]
    [ProducesResponseType<ResponseMessage>(400)]
    public object Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return StatusCode(400, new ResponseMessage("Name is required"));
        }
        
        var host = HttpContext.RequestServices.GetRequiredService<TGameHost>();
        host.Name = name;
        HostRegistry.Add(host);
        return new GameInfo
        {
            Id = host.Name
        };
    }

    [HttpPost("create")]
    [RequireUser]
    [ProducesResponseType<GameInfo>(200)]
    [ProducesResponseType<ResponseMessage>(400)]
    public object Create() => Create(Guid.NewGuid().ToString("N"));
    
    [HttpPost("games/{name}/start")]
    [RequireUser]
    [ProducesResponseType<GameInfo>(200)]
    [ProducesResponseType<ResponseMessage>(404)]
    public async Task<object> Start(string name)
    {
        if (!HostRegistry.TryGet<TGameHost>(name, out var host))
        {
            return StatusCode(404, new ResponseMessage($"Game not found: '{name}'"));
        }
        
        await host.StartAsync();
        return StatusCode(200, new ResponseMessage($"Game '{name}' started"));
    }
    
    [HttpGet("join/{gameName}")]
    [RequireUser]
    public async Task Join(string gameName)
    {
        //HttpContext.Response.Headers.Connection = "close";
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
        using var actionSocket = await HttpContext.WebSockets.AcceptWebSocketAsync(WebSocketDefaults.AcceptContext);
        
        if (!await HostRegistry.StartJoinAsync<TGameHost>(decksterUser, actionSocket, gameName))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Could not connect"));
            await actionSocket.CloseOutputAsync(WebSocketCloseStatus.ProtocolError, "Could not connect", default);
        }
    }

    [HttpGet("join/{connectionId:guid}/finish")]
    [RequireUser]
    public async Task FinishJoin(Guid connectionId)
    {
        //HttpContext.Response.Headers.Connection = "close";
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Not WS request"));
            return;
        }
        
        using var eventSocket = await HttpContext.WebSockets.AcceptWebSocketAsync(WebSocketDefaults.AcceptContext);

        if (!await HostRegistry.FinishJoinAsync(connectionId, eventSocket))
        {
            if (!HttpContext.Response.HasStarted)
            {
                HttpContext.Response.StatusCode = 400;
                await HttpContext.Response.WriteAsJsonAsync(new ResponseMessage("Could not connect"));    
            }
        }
    }
}

public static class WebSocketDefaults
{
    public static readonly WebSocketAcceptContext AcceptContext = new()
    {
        KeepAliveInterval = TimeSpan.FromSeconds(5)
    };
}