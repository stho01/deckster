using Deckster.Client.Games.Common;

namespace Deckster.Server.Games.CrazyEights.Core;

public class Deck
{
    public List<Card> Cards { get; }

    public Deck(IEnumerable<Card> cards)
    {
        Cards = cards.ToList();
    }

    public Deck Shuffle()
    {
        Cards.KnuthShuffle();
        return this;
    }
    
    public static Deck Standard
    {
        get
        {
            var cards = Enumerable.Range(1, 13).SelectMany(rank => Enum.GetValues<Suit>().Select(suit => new Card(rank, suit)));
            return new Deck(cards);
        }
    }
    
    
    
    
}


