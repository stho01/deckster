using System.Diagnostics;
using System.Text;
using Deckster.Client.Games.CrazyEights;

namespace Deckster.CrazyEights.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        if (!argz.Any())
        {
            PrintUsage();
            return 0;
        }
        
        try
        {
            var uri = new Uri(argz[0]);
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

    private static void PrintUsage()
    {
        var usage = new StringBuilder()
            .AppendLine("Usage:")
            .AppendLine($"{Process.GetCurrentProcess().ProcessName} <uri>")
            .AppendLine($"e.g {Process.GetCurrentProcess().ProcessName} deckster://localhost:23023/crazyeights/123456");
        Console.WriteLine(usage);
    }
}