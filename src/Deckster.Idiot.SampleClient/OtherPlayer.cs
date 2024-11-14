using Deckster.Core.Games.Common;

namespace Deckster.Idiot.SampleClient;

public class OtherPlayer
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public List<Card> KnownCardsOnHand { get; init; } = [];
    public int CardsOnHandCount { get; set; }
    public List<Card> CardsFacingUp { get; init; }
    public int CardsFacingDownCount { get; set; }
    public bool IsDone { get; set; }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}