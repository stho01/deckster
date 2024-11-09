using Deckster.Core.Serialization;
using Deckster.Games.Yaniv;
using Deckster.Server.Data;
using Deckster.Server.Games.Yaniv;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Yaniv;

public class YanivGameHostTest
{
    [Test]
    public async ValueTask Play()
    {
        var repo = new InMemoryRepo();
        var host = new YanivGameHost(repo);

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
            var thing = repo.EventThings.Values.Cast<InMemoryEventQueue<YanivGame>>().SingleOrDefault();
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