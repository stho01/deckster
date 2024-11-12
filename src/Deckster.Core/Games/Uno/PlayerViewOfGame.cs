using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Uno;

public class PlayerViewOfGame : DecksterResponse
{
    public List<UnoCard> Cards { get; init; } = [];
    public UnoCard TopOfPile { get; init; }
    public UnoColor CurrentColor { get; init; }
    public int StockPileCount { get; init; }
    public int DiscardPileCount { get; init; }
    public List<OtherUnoPlayer> OtherPlayers { get; init; } = [];

    public PlayerViewOfGame()
    {
        
    }

    public PlayerViewOfGame(string error)
    {
        Error = error;
    }
}