using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Yaniv;
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

    public override string ToString()
    {
        return $"{Name} ({Id})"; 
    }
}