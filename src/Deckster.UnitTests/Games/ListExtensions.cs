using Deckster.Client.Games.Common;

namespace Deckster.UnitTests.Games;

public static class ListExtensions
{
    public static Card Get(this List<Card> cards, int rank, Suit suit) => cards.Get(new Card(rank, suit));
    
    public static T Get<T>(this List<T> cards, T card)
    {
        if (!cards.Remove(card))
        {
            throw new InvalidOperationException($"List does not contain {card}");
        }

        return card;
    }
}