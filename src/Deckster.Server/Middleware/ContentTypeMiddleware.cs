using System.Diagnostics.CodeAnalysis;

namespace Deckster.Server.Middleware;

public class ContentTypeMiddleware
{
    private readonly Dictionary<string, string> _map = new()
    {
        [".json"] = "application/json",
        [".yaml"] = "text/yaml",
        [".yml"] = "text/yaml",
        [".html"] = "text/html",
        [".xml"] = "text/xml"
    };
    
    private readonly RequestDelegate _next;

    public ContentTypeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        if (TryGetExtension(context, out var path, out var extension) &&
            _map.TryGetValue(extension, out var contentType))
        {
            context.Request.Path = path;
            context.Request.Headers.ContentType = contentType;
        }

        return _next(context);
    }

    private static bool TryGetExtension(HttpContext context, out PathString newPath, [MaybeNullWhen(false)]out string extension)
    {
        newPath = default;
        extension = default;
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        for (var ii = path.Length - 1; ii >= 0; ii--)
        {
            var c = path[ii];
            switch (c)
            {
                case '.':
                    if (ii == path.Length - 1)
                    {
                        return false;
                    }
                    extension = path[ii..];
                    newPath = new PathString(path[..ii]);
                    return true;
                case '/':
                    return false;
                default:
                    continue;
            }
        }

        return false;
    }
}

public static class ContentTypeMiddlewareExtensions
{
    public static IApplicationBuilder MapExtensionToContentType(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ContentTypeMiddleware>();
    }
}