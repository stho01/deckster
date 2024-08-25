using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Deckster.Client;
using Microsoft.Extensions.Configuration;

namespace Deckster.CrazyEights.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)            ;
        var config = builder.Build();
        var settings = config.GetSection("Deckster").Get<DecksterSettings>();
        
        
        
        
        if (!TryGetUrl(argz, out _))
        {
            PrintUsage();
            return 0;
        }
        
        try
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, _) => cts.Cancel();
            
            var deckster = new DecksterClient(settings.ServerUrl, settings.Token);
            Console.WriteLine("Enter game name");
            var gamename = Console.ReadLine();
            var game = await deckster.Uno.EnsureAndJoinAsync(gamename, cts.Token);
            var noob = new UnoNoob(game);
            noob.StartPlaying();
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }

    private static bool TryGetUrl(string[] args, [MaybeNullWhen(false)] out Uri uri)
    {
        foreach (var a in args)
        {
            if (Uri.TryCreate(a, UriKind.Absolute, out uri))
            {
                return true;
            }
        }

        uri = new Uri($"ws://localhost:13992/crazyeights/join/{Guid.Empty}");
        return true;
    }

    private static void PrintUsage()
    {
        var usage = new StringBuilder()
            .AppendLine("Usage:")
            .AppendLine($"{Process.GetCurrentProcess().ProcessName} <uri>")
            .AppendLine($"e.g {Process.GetCurrentProcess().ProcessName} deckster://localhost:23023/123456");
        Console.WriteLine(usage);
    }
}