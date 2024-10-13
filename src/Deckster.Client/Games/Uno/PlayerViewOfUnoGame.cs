namespace Deckster.Client.Games.Uno;

public class PlayerViewOfUnoGame : UnoResponse
{
    public List<UnoCard> Cards { get; init; }
    public UnoCard TopOfPile { get; init; }
    public UnoColor CurrentSuit { get; init; }
    public int StockPileCount { get; init; }
    public int DiscardPileCount { get; init; }
    public List<OtherUnoPlayer> OtherPlayers { get; init; }
}