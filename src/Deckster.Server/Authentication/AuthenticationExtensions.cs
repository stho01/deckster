using System.Diagnostics.CodeAnalysis;
using Deckster.Server.Data;

namespace Deckster.Server.Authentication;

public static class AuthenticationExtensions
{
    public static void SetUser(this HttpContext context, DecksterUser user)
    {
        context.Items["User"] = user;
    }
    
    public static DecksterUser? GetUser(this HttpContext context)
    {
        return context.Items.TryGetValue("User", out var o) && o is DecksterUser u ? u : null;
    }

    public static bool TryGetUser(this HttpContext context, [MaybeNullWhen(false)] out DecksterUser user)
    {
        if (context.Items.TryGetValue("User", out var o) && o is DecksterUser u)
        {
            user = u;
            return true;
        }

        user = null;
        return false;
    }
    
    public static DecksterUser GetRequiredUser(this HttpContext context)
    {
        if (context.Items.TryGetValue("User", out var o) && o is DecksterUser u)
        {
            return u;
        }

        throw new ApplicationException("User is required");
    }

    public static IApplicationBuilder LoadUser(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserLoaderMiddleware>();
    }
}