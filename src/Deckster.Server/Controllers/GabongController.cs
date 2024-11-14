using Deckster.Games.Gabong;
using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.Gabong;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("gabong")]
public class GabongController(GameHostRegistry hostRegistry, IRepo repo)
    : GameController<GabongGameHost, GabongGame>(hostRegistry, repo);