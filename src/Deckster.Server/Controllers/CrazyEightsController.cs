using Deckster.Server.Authentication;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("crazyeights")]
[RequireUser]
public class CrazyEightsController : CardGameController<CrazyEightsGameHost>
{
    public CrazyEightsController(GameHostRegistry hostRegistry) : base(hostRegistry)
    {
    }
}