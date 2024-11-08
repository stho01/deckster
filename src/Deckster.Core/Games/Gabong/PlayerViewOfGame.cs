using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Gabong;

public class PlayerViewOfGame : GabongResponse
{
    public List<Card> Cards { get; init; } = [];
    public Card TopOfPile { get; set; }
    public Suit CurrentSuit { get; set; }
    public int StockPileCount { get; set; }
    public int DiscardPileCount { get; set; }
    
    public Guid LastPlayMadeByPlayerId { get; set; }
    public GabongPlay LastPlay { get; set; }
    public List<OtherGabongPlayer> OtherPlayers { get; init; } = [];
    public List<Guid> PlayersOrder { get; init; } = [];

    public PlayerViewOfGame() { }

    
    public PlayerViewOfGame(string error)
    {
        Error = error;
    }

    public PlayerViewOfGame WithCardsAddedNotification(Card drawnCard)
    {
        CardsAdded.Add(drawnCard);
        return this;
    }
}