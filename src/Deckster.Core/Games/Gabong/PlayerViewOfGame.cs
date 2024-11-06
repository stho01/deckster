using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Gabong;

public class PlayerViewOfGame : DecksterResponse
{
    public List<Card> Cards { get; init; } = [];
    public Card TopOfPile { get; init; }
    public Suit CurrentSuit { get; init; }
    public int StockPileCount { get; init; }
    public int DiscardPileCount { get; init; }
    public List<OtherGabongPlayer> OtherPlayers { get; init; } = [];

    public PlayerViewOfGame()
    {
        
    }

    public PlayerViewOfGame(string error)
    {
        Error = error;
    }
}