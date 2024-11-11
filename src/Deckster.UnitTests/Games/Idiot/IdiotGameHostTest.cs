using Deckster.Core.Serialization;
using Deckster.Games.Idiot;
using Deckster.Games.Uno;
using Deckster.Server.Data;
using Deckster.Server.Games.Idiot;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Idiot;

public class IdiotGameHostTest
{
    [Test]
    public async ValueTask RunGame()
    {
        for (var ii = 0; ii < 500; ii++)
        {
            await DoRunGameAsync();
        }
        
    }

    [Test]
    public async Task DoRunGameAsync()
    {
        var repo = new InMemoryRepo();
        var host = new IdiotGameHost(repo, new NullLoggerFactory());

        for (var ii = 0; ii < 4; ii++)
        {
            if (!host.TryAddBot(out var error))
            {
                Assert.Fail(error);
            }    
        }
        
        try
        {
            var timeout = Task.Delay(20000);
            var game = host.RunAsync();
            if (await Task.WhenAny(timeout, game) == timeout)
            {
                await host.EndAsync();
                throw new Exception("Timeout!");
            }
        }
        catch (Exception e)
        {
            var thing = repo.EventThings.Values.Cast<InMemoryEventQueue<IdiotGame>>().SingleOrDefault();
            if (thing != null)
            {
                foreach (var evt in thing.Events.ToList())
                {
                    Console.WriteLine(evt.Pretty());
                }
            }
        }
    }
}