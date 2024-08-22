using Deckster.Server.Games.CrazyEights;
using Deckster.Server.Games.CrazyEights.Core;
using NUnit.Framework;

namespace Deckster.UnitTests;

[TestFixture]
public class ShuffleTest
{
    [Test]
    public void KnuthShuffleTest()
    {
        var unshuffled = Deck.Standard.Cards;
        var shuffled = Deck.Standard.Cards.KnuthShuffle();

        Assert.That(unshuffled.SequenceEqual(shuffled), Is.False);
    }
}