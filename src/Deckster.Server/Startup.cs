using System.Text.Json.Serialization;
using Deckster.Client.Logging;
using Deckster.Core.Protocol;
using Deckster.Core.Serialization;
using Deckster.Games.CodeGeneration;
using Deckster.Games.CodeGeneration.Meta;
using Deckster.Server.Authentication;
using Deckster.Server.Configuration;
using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Middleware;
using Marten;
using Marten.Events.Projections;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.OpenApi.Models;
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
            o.SchemaGeneratorOptions.SupportNonNullableReferenceTypes = true;
            o.SchemaGeneratorOptions.NonNullableReferenceTypesAsRequired = true;
            o.SchemaGeneratorOptions.DiscriminatorNameSelector = t => t.InheritsFrom<DecksterMessage>() ? "type" : null;
            o.SchemaGeneratorOptions.DiscriminatorValueSelector = t => t.GetGameNamespacedName();
            o.SchemaGeneratorOptions.SchemaIdSelector =
                t => t.InheritsFrom<DecksterMessage>() ? t.GetGameNamespacedName() : t.Name;

            

            var mappings = new Dictionary<Type, Func<OpenApiSchema>>();
            var dictionary = new OpenApiSchemaGenerator(typeof(DecksterMessage)).Types;
            foreach (var (key, value) in dictionary)
            {
                mappings[key] = () => value;
            }
            
            o.SchemaGeneratorOptions.CustomTypeMappings = mappings;
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