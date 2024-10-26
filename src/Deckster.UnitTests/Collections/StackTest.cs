using NUnit.Framework;

namespace Deckster.UnitTests.Collections;

public class StackTest
{
    [Test]
    public void Stack()
    {
        var stack = new Stack<string>(new []{"1", "2"});
        
        Console.WriteLine(string.Join(", ", stack));
    }
}