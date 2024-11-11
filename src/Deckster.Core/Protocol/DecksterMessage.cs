using Deckster.Core.Serialization;

namespace Deckster.Core.Protocol;

public abstract class DecksterMessage
{
    /// <summary>
    /// Used for very internal, very important and very secret operations. Nothing to see here. Move along.
    /// </summary>
    public string Type => GetType().GetGameNamespacedName();
}

public abstract class DecksterRequest : DecksterMessage
{
    public Guid PlayerId { get; set; }

    public override string ToString()
    {
        return $"{GetType().Name} ({PlayerId})";
    }
}
public abstract class DecksterNotification : DecksterMessage;

public abstract class DecksterResponse : DecksterMessage
{
    /// <summary>
    /// returns true if has error, false if not. Best regards, Captain Obvious.
    /// </summary>
    public bool HasError => Error != null;
    
    /// <summary>
    /// Error message, if player has done something terribly wrong.
    /// </summary>
    public string? Error { get; init; }
}