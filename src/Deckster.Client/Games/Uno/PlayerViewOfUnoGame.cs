using Deckster.Client.Common;
using Deckster.Client.Games.Common;

namespace Deckster.Client.Games.Uno;

public class PlayerViewOfUnoGame : SuccessResponse
{
    public List<UnoCard> Cards { get; init; }
    public UnoCard TopOfPile { get; init; }
    public UnoColor CurrentSuit { get; init; }
    public int StockPileCount { get; init; }
    public int DiscardPileCount { get; init; }
    public List<OtherUnoPlayer> OtherPlayers { get; init; }
}