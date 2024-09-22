using System.Text;

namespace Deckster.Server.Controllers;

public static class HttpRequestExtensions
{
    public static string GetSchemeHostAndBasePath(this HttpRequest request)
    {
        var builder = new StringBuilder()
            .Append($"{request.Scheme}://{request.Host}");
        if (request.PathBase.HasValue)
        {
            builder.Append(request.PathBase);
        }

        return builder.ToString();
    }

    public static bool Accepts(this HttpRequest request, string contenttype)
    {
        var accept = request.Headers.Accept; 
        return accept.Count > 0 && accept.Any(a => a.Contains(contenttype, StringComparison.OrdinalIgnoreCase));
    }
}