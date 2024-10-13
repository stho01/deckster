using Deckster.Client.Serialization;

namespace Deckster.Client.Protocol;

public abstract class DecksterMessage
{
    public string Type => GetType().GetGameNamespacedName();
}

public abstract class DecksterRequest : DecksterMessage;
public abstract class DecksterNotification : DecksterMessage;
public abstract class DecksterResponse : DecksterMessage;