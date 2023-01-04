namespace Deckster.Client.Common;

public abstract class CommandResult : IHaveDiscriminator
{
    public string Discriminator => GetType().Name;
}