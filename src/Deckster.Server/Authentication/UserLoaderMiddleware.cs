using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Deckster.Server.Data;
using Microsoft.AspNetCore.Authentication;

namespace Deckster.Server.Authentication;

public class UserLoaderMiddleware
{
    private readonly IRepo _repo;
    private readonly RequestDelegate _next;

    public UserLoaderMiddleware(IRepo repo, RequestDelegate next)
    {
        _next = next;
        _repo = repo;
    }

    public Task Invoke(HttpContext context)
    {
        return TryGetToken(context, out var token)
            ? AuthenticateWithTokenAsync(context, token)
            : AuthenticateWithCookieAsync(context);
    }

    private async Task AuthenticateWithCookieAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(AuthenticationSchemes.Cookie);
        if (result.Succeeded)
        {
            var sub = result.Principal.FindFirstValue("sub");
            if (sub != null && Guid.TryParse(sub, out var id))
            {
                var user = await _repo.GetAsync<DecksterUser>(id, context.RequestAborted);
                if (user != null)
                {
                    context.SetUser(user);
                }
            }
        }
        await _next(context);
    }

    private async Task AuthenticateWithTokenAsync(HttpContext context, string token)
    {
        var user = await _repo.Query<DecksterUser>().FirstOrDefaultAsync(u => u.AccessToken == token);
        if (user != null)
        {
            context.SetUser(user);
        }

        await _next(context);
    } 

    private static bool TryGetToken(HttpContext context, [MaybeNullWhen(false)] out string token)
    {
        token = default;
        if (context.Request.Headers.TryGetValue("Authorization", out var values) && values.Any())
        {
            var value = values[0];
            if (value == null || !value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                
                return false;
            }

            token = value["Bearer ".Length..];
            return true;
        }

        return false;
    }
}