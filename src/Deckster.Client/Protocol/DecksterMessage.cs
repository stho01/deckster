using Deckster.Client.Communication;

namespace Deckster.Client.Protocol;

[JsonDerived<DecksterMessage>]
public abstract class DecksterMessage : IHaveDiscriminator
{
    public string Type => GetType().Name;
}