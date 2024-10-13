using Deckster.Client.Games.Uno;
using Deckster.Server.Games;
using Deckster.Server.Games.Uno;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("uno")]
public class UnoController : CardGameController<UnoClient, UnoGameHost>
{
    public UnoController(GameHostRegistry hostRegistry) : base(hostRegistry)
    {
    }
}