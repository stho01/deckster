namespace Deckster.Client.Protocol;

internal interface IHaveDiscriminator
{
    // ReSharper disable once UnusedMemberInSuper.Global
    string Type { get; }
}