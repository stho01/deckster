using System.Text.Json.Serialization;
using Deckster.Client.Logging;
using Deckster.Core.Serialization;
using Deckster.Server.Authentication;
using Deckster.Server.Configuration;
using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Middleware;
using Marten;
using Marten.Events.Projections;
using Microsoft.AspNetCore.WebSockets;
using Weasel.Core;

namespace Deckster.Server;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration c)
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
                    o.Projections.Add<CrazyEightsProjection>(ProjectionLifecycle.Inline);
                    o.Connection(config.Repo.Marten.ConnectionString);
                    o.UseSystemTextJsonForSerialization(DecksterJson.Options, EnumStorage.AsString, Casing.CamelCase);
                    o.AutoCreateSchemaObjects = AutoCreate.All;
                });
                services.AddSingleton<IRepo, MartenRepo>();
                break;
        }

        services.AddDataProtection(o =>
        {
        });

        services.AddDeckster();

        var mvc = services.AddMvc().AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
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
        services.AddRouting(o =>
        {
            o.LowercaseUrls = true;
            o.LowercaseQueryStrings = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.DescribeAllParametersInCamelCase();
            o.UseAllOfForInheritance();
        });
    }
    
    public static void Configure(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            // o.SwaggerEndpoint("/swagger/deckster/swagger.json", "deckster");
            o.DocumentTitle = "Deckster";
            o.RoutePrefix = "swagger";
        });
        
        app.MapExtensionToAcceptHeader();
        app.UseAuthentication();
        app.LoadUser();
        app.UseWebSockets();
        app.UseRouting();
        
        app.UseEndpoints(e =>
        {
            e.MapControllers();
            e.MapSwagger();
        });

    }
}