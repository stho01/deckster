using Deckster.Client.Common;
using Deckster.Client.Games.Common;

namespace Deckster.Client.Games.CrazyEights;

public class PlayerViewOfGame : SuccessResult
{
    public List<Card> Cards { get; init; }
    public Card TopOfPile { get; init; }
    public Suit CurrentSuit { get; init; }
    public int StockPileCount { get; init; }
    public int DiscardPileCount { get; init; }
    public List<OtherCrazyEightsPlayer> OtherPlayers { get; init; }
}