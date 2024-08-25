using Deckster.Client.Common;
using Deckster.Client.Games.Common;

namespace Deckster.Client.Games.Uno;

public class CardResponse : SuccessResponse
{
    public UnoCard Card { get; init; }

    public CardResponse()
    {
        
    }

    public CardResponse(UnoCard card)
    {
        Card = card;
    }
}