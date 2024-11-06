using Deckster.Server.Bootstrapping;
using Deckster.Server.Configuration;

namespace Deckster.Server;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        try
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, _) =>
            {
                if (cts.IsCancellationRequested)
                {
                    return;
                }
                cts.Cancel();
            };
            var builder = WebApplication.CreateBuilder(argz);
            
            builder.Configuration.Configure(b =>
            {
                b.Sources.Clear();
                b.AddJsonFile("appsettings.json");
                b.AddJsonFile("appsettings.local.json", true);
                b.AddEnvironmentVariables();
            });
            
            var services = builder.Services;
            var config = builder.Configuration.Get<DecksterConfig>();
            if (config == null)
            {
                throw new Exception("OMG CANT HAZ CONFIGS CUS ITS NULLZ");
            }
            Startup.ConfigureServices(services, config);

            await using var web = builder.Build();
            Startup.Configure(web);

            await web.RunAsync(cts.Token);
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Unhandled: {e}");
            return 1;
        }
    }
}