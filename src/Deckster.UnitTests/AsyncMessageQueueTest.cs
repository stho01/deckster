using System.Net.NetworkInformation;
using Deckster.Server.Games.Common.Fakes;
using NUnit.Framework;

namespace Deckster.UnitTests;

public class AsyncMessageQueueTest
{
    [Test]
    public async ValueTask Hello()
    {
        var pipe = new AsyncMessageQueue<string>();
        
        pipe.Add("message");
        var gotten = await pipe.ReadAsync();
        
        Assert.That(gotten, Is.EqualTo("message"));
    }

    [Test]
    public async ValueTask Hello2()
    {
        var pipe = new AsyncMessageQueue<string>();
        
        pipe.Add("one");
        pipe.Add("two");
        
        var gotten = await pipe.ReadAsync();
        Assert.That(gotten, Is.EqualTo("one"));
        pipe.Add("three");
        
        gotten = await pipe.ReadAsync();
        Assert.That(gotten, Is.EqualTo("two"));
        
        gotten = await pipe.ReadAsync();
        Assert.That(gotten, Is.EqualTo("three"));
    }

    [Test]
    public async ValueTask CancelTest()
    {
        var pipe = new AsyncMessageQueue<string>();
        
        using var cts = new CancellationTokenSource();
        var get = pipe.ReadAsync(cts.Token);
        cts.Cancel(true);
        try
        {
            var gotten = await get;
            Assert.Fail("Task should have been cancelled");
        }
        catch (TaskCanceledException e)
        {
            if (e.CancellationToken != cts.Token)
            {
                Assert.Fail("Task was cancelled for other reasons.");
            }
        }
    }
}