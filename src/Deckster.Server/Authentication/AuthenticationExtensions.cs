using Deckster.Server.Users;

namespace Deckster.Server.Authentication;

public static class AuthenticationExtensions
{
    public static void SetUser(this HttpContext context, User user)
    {
        context.Items["User"] = user;
    }
    
    public static User? GetUser(this HttpContext context)
    {
        return context.Items.TryGetValue("User", out var o) && o is User u ? u : null;
    }
    
    public static User GetRequiredUser(this HttpContext context)
    {
        if (context.Items.TryGetValue("User", out var o) && o is User u)
        {
            return u;
        }

        throw new ApplicationException("User is required");
    }

    public static IApplicationBuilder AddUserTokenAuthentication(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserTokenAuthenticationMiddleware>();
    }
}