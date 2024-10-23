using Deckster.Client.Games.Common;

namespace Deckster.Server.Games.CrazyEights.Core;

public class Decks
{
    public List<Card> Cards { get; }

    public Decks(IEnumerable<Card> cards)
    {
        Cards = cards.ToList();
    }
    
    public static List<Card> Standard
    {
        get
        {
            var cards = Enumerable.Range(1, 13).SelectMany(rank => Enum.GetValues<Suit>().Select(suit => new Card(rank, suit)));
            return cards.ToList();
        }
    }
}
