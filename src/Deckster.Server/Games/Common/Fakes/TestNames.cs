using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.Common.Fakes;

public static class TestNames
{
    private static readonly string[] Names;

    static TestNames()
    {
        var content = ReadEmbedded("testusers.txt");
        Names = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static string ReadEmbedded(string file)
    {
        var assembly = typeof(TestNames).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(file));
        if (name == null)
        {
            throw new Exception($"OMG CANT HAZ EMBDEBED FILEZ '{file}'");
        }

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream == null)
        {
            throw new Exception("OMG RESAURZ STREEM IZ NULLZ");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string Random() => Names.Random();
}