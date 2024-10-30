using Deckster.Client.Games.Common;

namespace Deckster.Server.Games;

public static class Decks
{
    public static List<Card> Standard
    {
        get
        {
            var cards = Enumerable.Range(1, 13).SelectMany(rank => Enum.GetValues<Suit>().Select(suit => new Card(rank, suit)));
            return cards.ToList();
        }
    }
}
