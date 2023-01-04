using Deckster.Client.Communication;
using Deckster.CrazyEights;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Infrastructure;
using Deckster.Server.Users;

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
            ConfigureWeb(web);
            
            using var decksterServer = DecksterServerBuilder.Create(DecksterConstants.TcpPort, web.Services)
                .UseMiddleware<CrazyEightsMiddleware>()
                .Build();

            await Task.WhenAny(web.RunAsync(cts.Token), decksterServer.RunAsync(cts.Token));
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unhandled: {e}");
            return 1;
        }
    }

    private static void ConfigureWeb(WebApplication app)
    {
        app.MapControllers();
        app.UseAuthentication();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddSingleton<UserRepo>();
        services.AddCrazyEights();
    }
}