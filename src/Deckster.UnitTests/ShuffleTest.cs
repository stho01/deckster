using Deckster.Server.Games.CrazyEights.Core;
using NUnit.Framework;

namespace Deckster.UnitTests;

[TestFixture]
public class ShuffleTest
{
    [Test]
    [TestCase(0)]
    [TestCase(Some.Seed)]
    [TestCase(-1)]
    public void KnuthShuffleTest(int seed)
    {
        var unshuffled = Decks.Standard;
        var shuffled = Decks.Standard.KnuthShuffle(seed);

        Assert.That(unshuffled.SequenceEqual(shuffled), Is.False);
    }
}