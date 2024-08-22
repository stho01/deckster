using Deckster.Client.Communication;
using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.CrazyEights;

public class PutCardRequest : DecksterRequest
{
    public Card Card { get; set; }
}

public class PutEightRequest : DecksterRequest
{
    public Card Card { get; set; }
    public Suit NewSuit { get; set; }
}

public class DrawCardRequest : DecksterRequest
{
    
}

public class PassRequest : DecksterRequest
{
    
}