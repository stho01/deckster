using Deckster.Server.Authentication;
using Deckster.Server.Data;
using Deckster.Server.Games.CrazyEights;
using Microsoft.AspNetCore.WebSockets;

namespace Deckster.Server;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        try
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, _) => cts.Cancel();
            var builder = WebApplication.CreateBuilder(argz);
            

            var services = builder.Services;
            ConfigureServices(services);

            await using var web = builder.Build();
            Configure(web);

            await web.RunAsync(cts.Token);
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unhandled: {e}");
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(b => b.AddConsole());
        services.AddWebSockets(o =>
        {
            o.KeepAliveInterval = TimeSpan.FromSeconds(10);
        });
        
        services.AddControllers();
        services.AddSingleton<IRepo, InMemoryRepo>();

        services.AddCrazyEights();

        var mvc = services.AddMvc();
        mvc.AddRazorRuntimeCompilation();
        
        services.AddAuthentication(o =>
            {
                o.DefaultScheme = AuthenticationSchemes.Cookie;
            })
            .AddCookie(AuthenticationSchemes.Cookie, o =>
            {
                o.LoginPath = "/login";
                o.LogoutPath = "/logout";
                o.Cookie.Name = "deckster";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.IsEssential = true;
                o.Cookie.MaxAge = TimeSpan.FromDays(180);
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.SlidingExpiration = true;
                o.ExpireTimeSpan = TimeSpan.FromDays(180);
            });

    }
    
    private static void Configure(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseAuthentication();
        app.LoadUser();
        app.UseWebSockets();
        app.MapControllers();
    }
}

