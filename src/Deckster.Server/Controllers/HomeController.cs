using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("")]
public class HomeController : Controller
{
    [HttpGet("")]
    public object Index()
    {
        return "Pølse";
    }
}