using Deckster.Core.Serialization;
using NUnit.Framework;

namespace Deckster.UnitTests;

[TestFixture]
public class NameTest
{
    [Test]
    public void Name()
    {
        var namespacedName = GetType().GetGameNamespacedName();
        Assert.That(namespacedName, Is.EqualTo("UnitTests.NameTest"));
    }
}