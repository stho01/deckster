using Deckster.Client.Serialization;

namespace Deckster.Client.Protocol;

[JsonDerived<DecksterNotification>]
public abstract class DecksterNotification : IHaveDiscriminator
{
    public string Type => GetType().GetGameNamespacedName();
}