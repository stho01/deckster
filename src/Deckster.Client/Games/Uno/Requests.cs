using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Uno;

public abstract class UnoRequest : DecksterRequest;

public class PutCardRequest : UnoRequest
{
    public UnoCard Card { get; set; }
}

public class PutWildRequest : UnoRequest
{
    public UnoCard Card { get; set; }
    public UnoColor NewColor { get; set; }
}

public class ReadyToPlayRequest : UnoRequest;

public class DrawCardRequest : UnoRequest;

public class PassRequest : UnoRequest;