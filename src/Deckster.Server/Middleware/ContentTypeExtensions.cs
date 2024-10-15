namespace Deckster.Server.Middleware;

public static class ContentTypeExtensions
{
    public static bool Accepts(this HttpRequest request, string contenttype)
    {
        var accept = request.Headers.Accept; 
        return accept.Count > 0 && accept.Any(a => a!= null && a.Contains(contenttype, StringComparison.OrdinalIgnoreCase));
    }

    public static bool AcceptsJson(this HttpRequest request) => request.Accepts("application/json");
    public static bool AcceptsYaml(this HttpRequest request) => request.Accepts("/yaml");
}