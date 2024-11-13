using Deckster.Core.Games.Common;

namespace Deckster.Core.Games.Yaniv;

public static class YanivCardExtensions
{
    public static int SumYanivPoints(this IList<Card> cards)
    {
        return cards.Sum(GetYanivPoints);
    }

    public static int GetYanivPoints(this Card card)
    {
        return card.Rank switch
        {
            0 - 9 => card.Rank,
            10 - 13 => 10,
            _ => card.Rank
        };
    }
}