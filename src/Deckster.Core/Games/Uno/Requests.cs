using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Uno;

public class PutCardRequest : DecksterRequest
{
    public UnoCard Card { get; set; }
}

public class PutWildRequest : DecksterRequest
{
    public UnoCard Card { get; set; }
    public UnoColor NewColor { get; set; }
}

public class DrawCardRequest : DecksterRequest;

public class PassRequest : DecksterRequest;