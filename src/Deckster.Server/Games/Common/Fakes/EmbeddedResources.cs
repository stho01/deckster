namespace Deckster.Server.Games.Common.Fakes;

internal static class EmbeddedResources
{
    public static string[] ReadLines(string file) => Read(file).Split(new[]{'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
    
    public static string Read(string file)
    {
        var assembly = typeof(TestUserNames).Assembly;
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
}