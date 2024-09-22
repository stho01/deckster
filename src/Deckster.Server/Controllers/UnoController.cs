using Deckster.Server.Authentication;
using Deckster.Server.Games;
using Deckster.Server.Games.Uno;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("uno")]
[RequireUser]
public class UnoController : CardGameController<UnoGameHost>
{
    public UnoController(GameRegistry registry) : base(registry)
    {
    }
}