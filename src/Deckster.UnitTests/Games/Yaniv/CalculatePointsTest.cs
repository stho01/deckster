using Deckster.Core.Games.Common;
using Deckster.Core.Games.Yaniv;
using Deckster.Games.Yaniv;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Yaniv;

public class CalculatePointsTest
{
    [Test]
    [TestCase(0,0)]
    [TestCase(1,1)]
    [TestCase(2,2)]
    [TestCase(3,3)]
    [TestCase(4,4)]
    [TestCase(5,5)]
    [TestCase(6,6)]
    [TestCase(7,7)]
    [TestCase(8,8)]
    [TestCase(9,9)]
    [TestCase(10,10)]
    [TestCase(11,11)]
    [TestCase(12,12)]
    [TestCase(13,13)]
    public void Calculate(int rank, int expectedPoints)
    {
        Assert.That(new Card(rank, Suit.Diamonds).GetYanivPoints(), Is.EqualTo(expectedPoints));
    }

    [Test]
    public void Sum()
    {
        var cards = new[] {new Card(0, Suit.Diamonds), new Card(5, Suit.Clubs), new Card(13, Suit.Spades)};
        Assert.That(cards.SumYanivPoints(), Is.EqualTo(18));
    }
}