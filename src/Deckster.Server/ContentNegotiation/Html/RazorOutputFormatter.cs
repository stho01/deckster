using Deckster.Server.Middleware;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Deckster.Server.ContentNegotiation.Html;

public class RazorOutputFormatter : IOutputFormatter
{
    public bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
            
        var canWrite = context.HttpContext.Request.Accepts("html") &&
                       context.HttpContext.SearchView(context.Object).Success;
        return canWrite;
    }

    public Task WriteAsync(OutputFormatterWriteContext context)
    {
        return context.HttpContext.RenderRazorView(context.Object, CancellationToken.None);
    }
}

public readonly struct ViewSearchResult
{
    public bool Success => Found != null;
    public ViewEngineResult? Found { get; }
    public IList<ViewEngineResult> Failed { get; }

    public ViewSearchResult(IEnumerable<ViewEngineResult> results)
    {
        Found = null;
        Failed = new List<ViewEngineResult>();
        foreach (var result in results)
        {
            if (result.Success)
            {
                Found = result;
                break;
            }
            Failed.Add(result);
        }
    }
}