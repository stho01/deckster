using Deckster.Games;

namespace Deckster.Server.Games.Common.Fakes;

public static class TestUserNames
{
    private static readonly string[] Names = EmbeddedResources.ReadLines("testusers.txt");
    public static string Random() => Names.Random();
}