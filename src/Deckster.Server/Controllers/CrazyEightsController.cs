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
    public ViewResult Overview()
    {
        var games = Registry.GetGames<CrazyEightsGameHost>().Select(h => new GameVm
        {
            Id = h.Id,
            Players = h.GetPlayers()
        });
        return View(games);
    }

    [HttpGet("games")]
    public object Games()
    {
        var games = Registry.GetGames<CrazyEightsGameHost>().Select(h => new GameVm
        {
            Id = h.Id,
            Players = h.GetPlayers()
        });
        return games;
    }

    [HttpPost("create")]
    public object Create()
    {
        var host = new CrazyEightsGameHost();
        Registry.Add(host);
        return StatusCode(200, new GameVm
        {
            Id = host.Id,
            Players = []
        });
    }
}