using Deckster.Client.Games.CrazyEights;
using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Games.CrazyEights.Core;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("crazyeights")]
public class CrazyEightsController(GameHostRegistry hostRegistry, IRepo repo)
    : GameController<CrazyEightsClient, CrazyEightsGameHost, CrazyEightsGame>(hostRegistry, repo);