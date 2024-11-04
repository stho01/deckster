using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.CrazyEights;

public class PutCardRequest : DecksterRequest
{
    public Card Card { get; set; }
}

public class PutEightRequest : DecksterRequest
{
    public Card Card { get; set; }
    public Suit NewSuit { get; set; }
}

public class DrawCardRequest : DecksterRequest;

public class PassRequest : DecksterRequest;

public class CardResponse : DecksterResponse
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
