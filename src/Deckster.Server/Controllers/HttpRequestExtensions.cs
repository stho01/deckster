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
}