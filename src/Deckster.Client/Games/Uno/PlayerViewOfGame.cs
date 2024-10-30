namespace Deckster.Client.Games.Uno;

public class PlayerViewOfGame : UnoResponse
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