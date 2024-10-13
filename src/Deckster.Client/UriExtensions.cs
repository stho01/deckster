using System.Text;

namespace Deckster.Client;

public static class UriExtensions
{
    public static Uri Append(this Uri uri, string path)
    {
        var builder = new StringBuilder()
            .Append($"{uri.Scheme}://")
            .Append(uri.Authority);
        if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
        {
            builder.Append(uri.AbsolutePath);
        }

        builder.Append($"/{path.TrimStart('/')}");

        return new Uri(builder.ToString());
    }

    public static Uri ToWebSocketUri(this Uri uri, string path)
    {
        var scheme = uri.Scheme switch
        {
            "http" => "ws",
            "https" => "wss",
            _ => "ws"
        };
        var builder = new StringBuilder($"{scheme}://")
            .Append(uri.Authority);
        if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
        {
            builder.Append(uri.AbsolutePath);
        }

        builder.Append($"/{path.TrimStart('/')}");

        return new Uri(builder.ToString());
    }
}