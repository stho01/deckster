using Deckster.Client.Games.Common;
using Deckster.Server.Collections;

namespace Deckster.Server.Games.Idiot;

public class IdiotPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public List<Card> CardsOnHand { get; init; } = [];
    public List<Card> FaceUpTableCards { get; init; } = [];
    public List<Card> FaceDownTableCards { get; init; } = [];

    public bool IsStillPlaying() => CardsOnHand.Any() || FaceUpTableCards.Any() || FaceDownTableCards.Any();
    public bool IsDone() => !IsStillPlaying();
    
    public static readonly IdiotPlayer Null = new()
    {
        Id = Guid.Empty,
        Name = "Ing. Kognito"
    };

    public bool HasCardsOnHand(Card[] cards)
    {
        return cards.Any() && CardsOnHand.ContainsAll(cards);
    }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}