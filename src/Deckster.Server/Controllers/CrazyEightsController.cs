using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("crazyeights")]
public class CrazyEightsController : CardGameController<CrazyEightsGameHost>
{
    public CrazyEightsController(GameHostRegistry hostRegistry) : base(hostRegistry)
    {
    }
}