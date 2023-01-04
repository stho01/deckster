namespace Deckster.Client.Common;

internal interface IHaveDiscriminator
{
    // ReSharper disable once UnusedMemberInSuper.Global
    string Discriminator { get; }
}