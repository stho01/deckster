using Deckster.Client.Communication;

namespace Deckster.Client.Protocol;

[JsonDerived<DecksterResponse>]
public abstract class DecksterResponse : IHaveDiscriminator
{
    public string Type => GetType().Name;
}