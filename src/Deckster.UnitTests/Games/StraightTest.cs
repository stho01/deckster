using Deckster.Core.Games.Common;
using Deckster.Games;
using NUnit.Framework;

namespace Deckster.UnitTests.Games;

public class StraightTest
{
    [Test]
    public void IsStraight_IsFalse_ForEmptyList()
    {
        var cards = Enumerable.Empty<Card>().ToList();
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne), Is.False);
    }
    
    [Test]
    public void IsStraight()
    {
        var cards = Enumerable.Range(1, 3).Select(i => new Card(i, Suit.Clubs)).ToList();
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne));
    }
    
    [Test]
    public void IsStraight_WithJoker()
    {
        var cards = new Card[]
        {
            new(1, Suit.Diamonds),
            new(2, Suit.Clubs),
            new(0, Suit.Hearts),
            new(4, Suit.Spades)
        };
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne));
    }
    
    [Test]
    public void IsStraight_WithJokerFirst()
    {
        var cards = new Card[]
        {
            new(0, Suit.Hearts),
            new(11, Suit.Diamonds),
            new(12, Suit.Clubs),
            new(13, Suit.Spades)
        };
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne));
    }
    
    [Test]
    public void IsStraight_WithOnlyJokers()
    {
        var cards = new Card[]
        {
            new(0, Suit.Diamonds),
            new(0, Suit.Clubs),
            new(0, Suit.Hearts),
            new(0, Suit.Spades)
        };
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne));
    }
    
    [Test]
    public void IsStraight_IsFalse_WhenNotStraight()
    {
        var cards = new Card[]
        {
            new(1, Suit.Diamonds),
            new(1, Suit.Clubs),
            new(2, Suit.Hearts),
            new(3, Suit.Spades)
        };
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne), Is.False);
    }
    
    [Test]
    public void IsStraight_IsFalse_WhenNotStraight2()
    {
        var cards = new Card[]
        {
            new(1, Suit.Diamonds),
            new(3, Suit.Clubs),
            new(5, Suit.Hearts),
            new(8, Suit.Spades)
        };
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne), Is.False);
    }
    
    [Test]
    public void IsStraight_IsFalse_ForEmptyArray()
    {
        Card[] cards = [];
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne), Is.False);
    }
    
    [Test]
    public void IsStraight_IsTrue_ForOneCard()
    {
        Card[] cards = [new(1,Suit.Spades)];
        Assert.That(cards.IsStraight(ValueCaluclation.AceIsOne), Is.True);
    }
}