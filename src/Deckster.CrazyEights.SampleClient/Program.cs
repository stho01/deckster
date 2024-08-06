using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Deckster.Client.Games.CrazyEights;

namespace Deckster.CrazyEights.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        if (!TryGetUrl(argz, out var uri))
        {
            PrintUsage();
            return 0;
        }
        
        try
        {
            using var cts = new CancellationTokenSource();
            var client = await CrazyEightsClientFactory.ConnectAsync(uri, cts.Token);
            
            var ai = new CrazyEightsPoorAi(client);
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

        uri = default;
        return false;
    }

    private static void PrintUsage()
    {
        var usage = new StringBuilder()
            .AppendLine("Usage:")
            .AppendLine($"{Process.GetCurrentProcess().ProcessName} <uri>")
            .AppendLine($"e.g {Process.GetCurrentProcess().ProcessName} deckster://localhost:23023/crazyeights/123456");
        Console.WriteLine(usage);
    }
}