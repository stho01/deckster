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
    public async Task<object> Create()
    {
        var host = new CrazyEightsGameHost();
        Registry.Add(host);
        return StatusCode(200, new { host.Id });
    }

    [HttpPost("start/{id:guid}")]
    public async Task<object> Start(Guid id)
    {
        if (!Registry.TryGet(id, out var host))
        {
            return StatusCode(404, new ResponseMessage("Game not found: '{id}'"));
        }
        
        await host.Start();
        return StatusCode(200, new ResponseMessage("Game '{id}' started"));
    }
}
