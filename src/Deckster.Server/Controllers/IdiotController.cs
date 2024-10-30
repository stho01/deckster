using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.Idiot;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("idiot")]
public class IdiotController(GameHostRegistry hostRegistry, IRepo repo)
    : GameController<IdiotGameHost, IdiotGame>(hostRegistry, repo);