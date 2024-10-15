using System.Diagnostics.CodeAnalysis;

namespace Deckster.Server.Middleware;

public class AcceptHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public AcceptHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        if (TryGetExtension(context, out var path, out var extension) && AcceptHeaders.Map.TryGetValue(extension, out var contentType))
        {
            context.Request.Path = path;
            context.Request.Headers.Accept = contentType;
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

public static class AcceptHeaderMiddlewareExtensions
{
    public static IApplicationBuilder MapExtensionToAcceptHeader(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AcceptHeaderMiddleware>();
    }
}