using Deckster.Client.Serialization;

namespace Deckster.Client.Protocol;

[JsonDerived<DecksterRequest>]
public abstract class DecksterRequest : IHaveDiscriminator
{
    public string Type => GetType().GetGameNamespacedName();
}

