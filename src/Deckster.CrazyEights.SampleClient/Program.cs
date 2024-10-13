using Deckster.Client;
using Deckster.Client.Games.CrazyEights;

namespace Deckster.CrazyEights.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        try
        {
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => cts.Cancel();
            
            var deckster = await DecksterClient.LogInOrRegisterAsync("http://localhost:13992", "Kamuf Larsen", "hest");
            
            await using var game = await deckster.CrazyEights().CreateAndJoinAsync("my-game", cts.Token);

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
}