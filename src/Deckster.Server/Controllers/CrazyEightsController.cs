using Deckster.Server.Authentication;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("crazyeights")]
[RequireUser]
public class CrazyEightsController : CardGameController
{
    public CrazyEightsController(GameRegistry registry) : base(registry)
    {
    }

    [HttpGet("")]
    public ViewResult Index()
    {
        return View();
    }

    [HttpPost("create")]
    public object Create()
    {
        var host = new CrazyEightsGameHost();
        Registry.Add(host);
        return StatusCode(200, new { host.Id });
    }
}
