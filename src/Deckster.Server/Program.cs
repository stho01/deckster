using Deckster.Client.Communication;
using Deckster.Client.Logging;
using Deckster.Server.Authentication;
using Deckster.Server.Bootstrapping;
using Deckster.Server.Configuration;
using Deckster.Server.Data;
using Deckster.Server.Games.CrazyEights;
using Marten;
using Microsoft.AspNetCore.WebSockets;
using Weasel.Core;

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
            
            builder.Configuration.Configure(b =>
            {
                b.Sources.Clear();
                b.AddJsonFile("appsettings.json");
                b.AddJsonFile("appsettings.local.json", true);
                b.AddEnvironmentVariables();
            });
            
            var services = builder.Services;
            ConfigureServices(services, builder.Configuration);

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

    private static void ConfigureServices(IServiceCollection services, IConfiguration c)
    {
        var config = c.Get<DecksterConfig>() ?? throw new Exception("OMG NOT CONFIGZ");
        var logger = Log.Factory.CreateLogger<Program>();
        services.AddSingleton(config);
        services.AddLogging(b => b.AddConsole());
        services.AddWebSockets(o =>
        {
            o.KeepAliveInterval = TimeSpan.FromSeconds(10);
        });
        
        services.AddControllers();

        logger.LogInformation("Using {type} repo", config.Repo.Type);
        switch (config.Repo.Type)
        {
            case RepoType.InMemory:
                services.AddSingleton<IRepo, InMemoryRepo>();
                break;
            case RepoType.Marten:
                
                services.AddMarten(o =>
                {
                    o.Connection(config.Repo.Marten.ConnectionString);
                    o.UseSystemTextJsonForSerialization(DecksterJson.Options, EnumStorage.AsString, Casing.CamelCase);
                    o.AutoCreateSchemaObjects = AutoCreate.All;
                });
                services.AddSingleton<IRepo, MartenRepo>();
                break;
        }
        

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

