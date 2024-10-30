using Deckster.Client.Games.Common;

namespace Deckster.UnitTests.Games;

public static class ListExtensions
{
    public static Card Steal(this List<Card> cards, int rank, Suit suit) => cards.Steal(new Card(rank, suit));
    
    public static T Steal<T>(this List<T> cards, T card)
    {
        if (!cards.Remove(card))
        {
            throw new InvalidOperationException($"List does not contain {card}");
        }

        return card;
    }
}