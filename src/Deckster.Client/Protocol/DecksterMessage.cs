using Deckster.Client.Serialization;

namespace Deckster.Client.Protocol;

public abstract class DecksterMessage
{
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
public abstract class DecksterResponse : DecksterMessage;