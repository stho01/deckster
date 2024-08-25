using Deckster.Server.Authentication;
using Deckster.Server.Games;
using Deckster.Server.Games.Uno;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("uno")]
[RequireUser]
public class UnoController : CardGameController
{
    public UnoController(GameRegistry registry) : base(registry)
    {
    }

    [HttpGet("")]
    public ViewResult Index()
    {
        return View();
    }

    [HttpPost("create")]
    public object Create(GameNameHolder input)
    {
        var host = new UnoGameHost(input.GameName);
        Registry.Add(host);
        return StatusCode(200, new { host.Id });
    }
    
    [HttpPost("ensure")]
    [HttpGet("ensure")]
    public object Ensure(GameNameHolder input)
    {
        var existingId = Registry.ResolveGameId("Uno", input.GameName);
        if(existingId.HasValue)
        {
            return StatusCode(200, new { existingId });
        }
        return Create(input);
    }
}

public class GameNameHolder
{
    public string GameName { get; set; }
}
