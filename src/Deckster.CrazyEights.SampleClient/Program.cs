using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Deckster.Client;

namespace Deckster.CrazyEights.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        if (!TryGetUrl(argz, out _))
        {
            PrintUsage();
            return 0;
        }
        
        try
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => cts.Cancel();
            
            var deckster = new DecksterClient("http://localhost:13992", "abc123");
            
            await using var game = await deckster.CrazyEights.CreateAndJoinAsync("my-game", cts.Token);

            var ai = new CrazyEightsPoorAi(game);
            await ai.PlayAsync(cts.Token);
            
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