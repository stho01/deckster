using Deckster.Client.Communication;

namespace Deckster.Client.Protocol;

[JsonDerived<DecksterRequest>]
public abstract class DecksterRequest : IHaveDiscriminator
{
    public string Type => GetType().Name;
}

