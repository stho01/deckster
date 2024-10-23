using Deckster.Server.Games.CrazyEights.Core;
using NUnit.Framework;

namespace Deckster.UnitTests;

[TestFixture]
public class ShuffleTest
{
    [Test]
    public void KnuthShuffleTest()
    {
        var unshuffled = Decks.Standard;
        var shuffled = Decks.Standard.KnuthShuffle(Some.Seed);

        Assert.That(unshuffled.SequenceEqual(shuffled), Is.False);
    }
}