using Deckster.Core.Games.Common;
using Deckster.Games;
using NUnit.Framework;

namespace Deckster.UnitTests.Games;

public class SameRankExtensions
{
    [Test]
    public void SameRank()
    {
        var cards = new Card[]
        {
            new(1, Suit.Diamonds),
            new(1, Suit.Clubs),
            new(1, Suit.Hearts),
        };
        
        Assert.That(cards.AreOfSameRank());
    }
    
    [Test]
    public void SameRankWithJokers()
    {
        var cards = new Card[]
        {
            new(1, Suit.Diamonds),
            new(0, Suit.Spades),
            new(1, Suit.Clubs),
            new(1, Suit.Hearts),
        };
        
        Assert.That(cards.AreOfSameRank());
    }

    [Test]
    public void DifferentRank()
    {
        var cards = new Card[]
        {
            new(1, Suit.Diamonds),
            new(2, Suit.Spades),
            new(3, Suit.Clubs),
            new(4, Suit.Hearts),
        };
        
        Assert.That(cards.AreOfSameRank(), Is.False);
    }

    [Test]
    public void False_ForEmptyArray()
    {
        var cards = Array.Empty<Card>();
        Assert.That(cards.AreOfSameRank(), Is.False);
    }
}