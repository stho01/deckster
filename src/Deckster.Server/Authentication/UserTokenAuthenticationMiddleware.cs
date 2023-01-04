using Deckster.Server.Users;

namespace Deckster.Server.Authentication;

public class UserTokenAuthenticationMiddleware
{
    private readonly UserRepo _users;
    private readonly RequestDelegate _next;
    

    public UserTokenAuthenticationMiddleware(UserRepo users, RequestDelegate next)
    {
        _next = next;
        _users = users;
    }

    public async Task Invoke(HttpContext context)
    {
        var token = GetToken(context);
        if (token != null)
        {
            var user = await _users.GetByTokenAsync(token);
            if (user != null)
            {
                context.SetUser(user);
            }
        }

        await _next(context);
    }

    private static string? GetToken(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out var values) && values.Any())
        {
            var value = values[0];
            if (value == null || !value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return value["Bearer ".Length..];

        }

        return null;
    }
}