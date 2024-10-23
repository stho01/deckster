using Deckster.Client.Serialization;
using Deckster.Server.Data;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Games.CrazyEights.Core;
using NUnit.Framework;

namespace Deckster.UnitTests.Games;

public class CrazyEightsGameHostTest
{
    [Test]
    public async ValueTask Game()
    {
        var repo = new InMemoryRepo();
        var host = new CrazyEightsGameHost(repo);

        for (var ii = 0; ii < 4; ii++)
        {
            if (!host.TryAddBot(out var error))
            {
                Assert.Fail(error);
            }    
        }
        Console.WriteLine("Starting");
        try
        {
            await host.StartAsync();
        
            Console.WriteLine("Running");
            while (host.State != GameState.Finished)
            {
                await Task.Delay(1000);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var thing = repo.EventThings.Values.Cast<InMemoryEventThing<CrazyEightsGame>>().SingleOrDefault();
            if (thing != null)
            {
                foreach (var evt in thing.Events)
                {
                    Console.WriteLine(evt.Pretty());
                }
            }
        }
        
    }
}