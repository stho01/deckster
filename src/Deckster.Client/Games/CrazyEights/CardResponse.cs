using Deckster.Client.Common;
using Deckster.Client.Games.Common;

namespace Deckster.Client.Games.CrazyEights;

public class CardResponse : SuccessResponse
{
    public Card Card { get; init; }

    public CardResponse()
    {
        
    }

    public CardResponse(Card card)
    {
        Card = card;
    }
}