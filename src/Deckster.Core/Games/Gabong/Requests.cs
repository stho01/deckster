using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Gabong;

public class PutCardRequest : DecksterRequest
{
    public Card Card { get; set; }
}

public class PutWildRequest : DecksterRequest
{
    public Card Card { get; set; }
    public Suit NewSuit { get; set; }
}

public class DrawCardRequest : DecksterRequest;
public class PassRequest : DecksterRequest;

public class GabongRequest : DecksterRequest;

public class BongaRequest : DecksterRequest;