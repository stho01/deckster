using Deckster.Client.Common;
using Deckster.Client.Games.Common;

namespace Deckster.Client.Games.Uno;

public class UnoCardsResponse : SuccessResponse
{
    public UnoCard Card { get; init; }

    public UnoCardsResponse()
    {
        
    }

    public UnoCardsResponse(UnoCard card)
    {
        Card = card;
    }
}