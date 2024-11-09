using Deckster.Core.Games.Common;

namespace Deckster.Server.Games;

public static class Decks
{
    public static List<Card> Jokers(int number)
    {
        if (number is < 0 or > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Number must be 1-4");
        }
        return Enum.GetValues<Suit>().Select(s => new Card(0, s)).Take(number).ToList();
    }

    public static List<Card> Standard()
    {
        var cards = Enumerable.Range(1, 13).SelectMany(rank => Enum.GetValues<Suit>().Select(suit => new Card(rank, suit)));
        return cards.ToList();
    }
}
