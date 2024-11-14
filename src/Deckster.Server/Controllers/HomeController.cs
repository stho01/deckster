using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Deckster.Core.Authentication;
using Deckster.Server.Authentication;
using Deckster.Server.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("")]
public class HomeController : Controller
{
    private static readonly HomeIndexModel HomeIndexModel;

    static HomeController()
    {
        var gameLinks = from c in typeof(HomeController).Assembly.GetTypes()
                    where typeof(IGameController).IsAssignableFrom(c) &&
                          !c.IsAbstract
            let name = c.Name.Replace("Controller", "")
            let route = c.GetCustomAttribute<RouteAttribute>()
            let link = new GameLink
                {
                    Name = name,
                    Href = route?.Template ?? name.ToLowerInvariant()
                }
            select link
            ;
        
        HomeIndexModel = new HomeIndexModel
        {
            GameTypes = gameLinks.ToArray()  
        };
    }
    
    private readonly IRepo _repo;

    public HomeController(IRepo repo)
    {
        _repo = repo;
    }

    [HttpGet("")]
    public object Index()
    {
        if (!HttpContext.TryGetUser(out _))
        {
            return RedirectToAction("login");
        }
        return HomeIndexModel;
    }

    [HttpGet("login")]
    public ViewResult Login()
    {
        return View();
    }

    [HttpPost("login")]
    public async Task<object> Login([FromBody] LoginModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Username))
        {
            return StatusCode(400, new ResponseMessage("Username must be specified"));
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            return StatusCode(400, new ResponseMessage("Password must be specified"));
        }

        var user = await _repo.Query<DecksterUser>().FirstOrDefaultAsync(u => string.Equals(u.Name, input.Username, StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            user = new DecksterUser
            {
                Name = input.Username,
                Password = input.Password,
                AccessToken = string.Join("", Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"))  
            };
            await _repo.SaveAsync(user);
        }

        if (input.Password != user.Password)
        {
            await Task.Delay(500, HttpContext.RequestAborted);
            return StatusCode(400, new ResponseMessage("Invalid credentials"));
        }

        var identity = new ClaimsIdentity([
            new Claim("sub", user.Id.ToString())
        ], AuthenticationSchemes.Cookie);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(AuthenticationSchemes.Cookie, principal, new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(180),
            IsPersistent = true
        });

        return StatusCode(200, new UserModel(user.Name, user.AccessToken));
    }
}

public class HomeIndexModel
{
    public GameLink[] GameTypes { get; init; } = [];
}

public class GameLink
{
    public string? Name { get; init; }
    public string? Href { get; init; }
}

public class LoginModel
{
    [JsonPropertyName("username")]
    public string? Username { get; init; }
    [JsonPropertyName("password")]
    public string? Password { get; init; }
}