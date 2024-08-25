using Deckster.Client.Communication;

namespace Deckster.Client.Protocol;

[JsonDerived<DecksterMessage>]
public abstract class DecksterMessage : IHaveDiscriminator
{
    protected virtual string Discriminator => "deckster";
    public string Type => $"{Discriminator}.{GetType().Name}";
}