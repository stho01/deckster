using Deckster.Games.Yaniv;
using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.Yaniv;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("yaniv")]
public class YanivController(GameHostRegistry hostRegistry, IRepo repo)
    : GameController<YanivGameHost, YanivGame>(hostRegistry, repo);