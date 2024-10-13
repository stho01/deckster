using Deckster.Client.Games.CrazyEights;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("crazyeights")]
public class CrazyEightsController : CardGameController<CrazyEightsClient, CrazyEightsGameHost>
{
    public CrazyEightsController(GameHostRegistry hostRegistry) : base(hostRegistry)
    {
    }
}