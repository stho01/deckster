using Deckster.Core.Serialization;
using Deckster.Games.CrazyEights;
using Deckster.Server.Data;
using Deckster.Server.Games.CrazyEights;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.CrazyEights;

public class CrazyEightsGameHostTest
{
    [Test]
    public async ValueTask RunGame()
    {
        var repo = new InMemoryRepo();
        var host = new CrazyEightsGameHost(repo, new NullLoggerFactory());

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
        var host = new CrazyEightsGameHost(repo, new NullLoggerFactory());

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