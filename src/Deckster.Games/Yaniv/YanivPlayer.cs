using Deckster.Core.Games.Common;
using Deckster.Games.Collections;

namespace Deckster.Games.Yaniv;

public class YanivPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public List<Card> CardsOnHand { get; init; } = [];
    public int SumOnHand => CardsOnHand.SumYanivPoints();
    public int Points { get; set; }
    public int Penalty { get; set; }
    public int TotalPoints => Points + Penalty;

    public static YanivPlayer Null => new YanivPlayer
    {
        Id = default,
        Name = "Ing. Kognito"
    };

    public bool HasCards(Card[] cards)
    {
        return CardsOnHand.ContainsAll(cards);
    }
}

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