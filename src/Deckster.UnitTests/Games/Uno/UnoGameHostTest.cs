using Deckster.Client.Serialization;
using Deckster.Server.Data;
using Deckster.Server.Games.Uno;
using Deckster.Server.Games.Uno.Core;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Uno;

public class UnoGameHostTest
{
    [Test]
    public async ValueTask RunGame()
    {
        var repo = new InMemoryRepo();
        var host = new UnoGameHost(repo);

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
            var thing = repo.EventThings.Values.Cast<InMemoryEventQueue<UnoGame>>().SingleOrDefault();
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