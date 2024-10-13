using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.CrazyEights;

public abstract class CrazyEightsRequest : DecksterRequest;

public class PutCardRequest : CrazyEightsRequest
{
    public Card Card { get; set; }
}

public class PutEightRequest : CrazyEightsRequest
{
    public Card Card { get; set; }
    public Suit NewSuit { get; set; }
}

public class DrawCardRequest : CrazyEightsRequest;

public class PassRequest : CrazyEightsRequest;

public abstract class CrazyEightsResponse : DecksterResponse;


public class CrazyEightsFailureResponse : CrazyEightsResponse
{
    public string Message { get; init; }

    public CrazyEightsFailureResponse()
    {
        
    }
    
    public CrazyEightsFailureResponse(string message)
    {
        Message = message;
    }
}

public class CardResponse : CrazyEightsResponse
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