using Deckster.Server.Authentication;
using Deckster.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("me")]
[RequireUser]
public class UserController : Controller
{
    private readonly IRepo _repo;

    public UserController(IRepo repo)
    {
        _repo = repo;
    }

    [HttpGet("")]
    public object Index()
    {
        var u = HttpContext.GetRequiredUser();
        return View(u);
    }
}