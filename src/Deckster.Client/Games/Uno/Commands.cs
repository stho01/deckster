using Deckster.Client.Communication;
using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Uno;

public class PutCardRequest : DecksterRequest
{
    public UnoCard Card { get; set; }
}

public class PutWildRequest : DecksterRequest
{
    public UnoCard Card { get; set; }
    public UnoColor NewColor { get; set; }
}

public class ReadyToPlayRequest : DecksterRequest
{
    
}

public class DrawCardRequest : DecksterRequest
{
    
}

public class PassRequest : DecksterRequest
{
    
}