namespace Deckster.Client.Games.Uno;

public static class UnoDecksterClientExtensions
{
    public static GameApi<UnoClient> Uno(this DecksterClient client)
    {
        return new GameApi<UnoClient>(client.BaseUri.Append("uno"), client.Token, c => new UnoClient(c));
    }
}