using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("crazyeights")]
public class CrazyEightsController(GameHostRegistry hostRegistry, IRepo repo)
    : GameController<CrazyEightsGameHost, CrazyEightsGame>(hostRegistry, repo);