using Deckster.Client.Serialization;
using Deckster.Server.Data;
using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Games.CrazyEights.Core;
using NUnit.Framework;

namespace Deckster.UnitTests.Games;

public class CrazyEightsGameHostTest
{
    [Test]
    public async ValueTask RunGame()
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
        
        try
        {
            Console.WriteLine("Starting");
            await host.RunAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var thing = repo.EventThings.Values.Cast<InMemoryEventQueue<CrazyEightsGame>>().SingleOrDefault();
            if (thing != null)
            {
                foreach (var evt in thing.Events)
                {
                    Console.WriteLine(evt.Pretty());
                }
            }
        }
    }

    [Test]
    public async ValueTask ReplayAsync()
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
        
        try
        {
            Console.WriteLine("Starting");
            var gameId = await host.RunAsync();
            if (!gameId.HasValue)
            {
                Assert.Fail("OMG GAEM AIDEE IZ NULLZ");
            }

            var game = await repo.GetGameAsync<CrazyEightsGame>(gameId.GetValueOrDefault(), 0);
            

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var thing = repo.EventThings.Values.Cast<InMemoryEventQueue<CrazyEightsGame>>().SingleOrDefault();
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