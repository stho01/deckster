namespace Deckster.Client.Games.CrazyEights;

public static class CrazyEightsDecksterClientExtensions
{
    public static GameApi<CrazyEightsClient> CrazyEights(this DecksterClient client)
    {
        return new GameApi<CrazyEightsClient>(client.BaseUri.Append("crazyeights"), client.Token, c => new CrazyEightsClient(c));
    }
}