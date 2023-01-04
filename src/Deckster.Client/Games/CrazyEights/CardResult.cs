using Deckster.Client.Common;
using Deckster.Client.Games.Common;

namespace Deckster.Client.Games.CrazyEights;

public class CardResult : SuccessResult
{
    public Card Card { get; init; }

    public CardResult()
    {
        
    }

    public CardResult(Card card)
    {
        Card = card;
    }
}