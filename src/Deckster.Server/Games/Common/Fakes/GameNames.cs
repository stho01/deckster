using Deckster.Games;

namespace Deckster.Server.Games.Common.Fakes;

public static class GameNames
{
    private static readonly string[] Adjectives = EmbeddedResources.ReadLines("adjectives.txt");
    private static readonly string[] Nouns = EmbeddedResources.ReadLines("nouns.txt");

    public static string Random() => $"{Adjectives.Random()} {Nouns.Random()}";
}