using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.CrazyEights;

public class PlayerViewOfGame : DecksterResponse
{
    public List<Card> Cards { get; init; }
    public Card TopOfPile { get; init; }
    public Suit CurrentSuit { get; init; }
    public int StockPileCount { get; init; }
    public int DiscardPileCount { get; init; }
    public List<OtherCrazyEightsPlayer> OtherPlayers { get; init; }
    
    public PlayerViewOfGame()
    {
        
    }
    
    public PlayerViewOfGame(string error)
    {
        Error = error;
    }
}