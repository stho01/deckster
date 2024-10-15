namespace Deckster.Server.Middleware;

public static class AcceptHeaders
{
    public static readonly Dictionary<string, string> Map = new()
    {
        [".json"] = "application/json",
        [".yaml"] = "application/yaml",
        [".yml"] = "application/yaml",
        [".html"] = "text/html",
        [".xml"] = "text/xml"
    };
}