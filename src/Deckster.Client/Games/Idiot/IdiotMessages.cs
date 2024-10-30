using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Idiot;

public abstract class IdiotRequest : DecksterRequest;
public abstract class IdiotResponse : DecksterResponse;

public class PlayerViewOfGame : IdiotResponse
{
    public List<Card> CardsOnHand { get; init; } = [];
    public Card? TopOfPile { get; init; }
    
    public int StockPileCount { get; init; }
    public int DiscardPileCount { get; init; }
    public List<OtherIdiotPlayer> OtherPlayers { get; init; } = [];
}

public class OtherIdiotPlayer
{
    public Guid PlayerId { get; init; }
    public string Name { get; init; }
    public int CardsOnHandCount { get; init; }
    public List<Card> VisibleTableCards { get; init; } = [];
    public int HiddenTableCardsCount { get; init; }
}