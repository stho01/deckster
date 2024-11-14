using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Deckster.Server.ContentNegotiation.Html;

public static class RazorViewNegotiator
{
    private static readonly EmptyTempDataProvider EmptyTempDataProvider = new();
        
    /// <summary>
    /// Finds a view and renders it.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static Task RenderRazorView(this HttpContext context, object? model, CancellationToken cancellationToken)
    {
        var searchResult = context.SearchView(model);
        if (searchResult is {Success: true, Found.View: not null})
        {
            var tempDataProvider = context.RequestServices.GetService<ITempDataProvider>() ?? EmptyTempDataProvider;
            var routeData = context.GetRouteData();
            var actionContext = new ActionContext(context, routeData, new ActionDescriptor(), new ModelStateDictionary());
            return context.Response.RenderView(searchResult.Found.View, model, actionContext, tempDataProvider, cancellationToken);
        }

        context.Response.StatusCode = 404;

        var message = new StringBuilder()
            .AppendLine("Could not find view.")
            .AppendLine()
            .AppendLine("Searched all over:");
        foreach (var location in searchResult.Failed.SelectMany(r => r.SearchedLocations))
        {
            message.AppendLine(location);
        }

        return context.Response.WriteAsync(message.ToString(), cancellationToken);
    }

    public static ViewSearchResult SearchView(this HttpContext context, object? model)
    {
        var viewEngine = context.RequestServices.GetRequiredService<IRazorViewEngine>();
        var routeData = context.GetRouteData();
        var actionContext = new ActionContext(context, routeData, new ActionDescriptor());
        var candidates = viewEngine.FindViewCandidates(actionContext, model);
        return new ViewSearchResult(candidates);
    }

    private static IEnumerable<ViewEngineResult> FindViewCandidates(this IViewEngine viewEngine, ActionContext context, object? model)
    {
        return GetViewNameCandidates(context.HttpContext, model).Select(n => viewEngine.FindView(context, n, true));
    }

    private static IEnumerable<string> GetViewNameCandidates(HttpContext context, object? model)
    {
        var modelName = model?.GetType().Name;
        var candidates = new[]
        {
            context.GetViewName(),
            modelName,
            modelName?.RemoveLastOccurrence("model", StringComparison.CurrentCultureIgnoreCase),
            context.GetActionName(),
            "Json"
        };
        foreach (var candidate in candidates)
        {
            if (candidate != null)
            {
                yield return candidate;
            }
        }
    }
        
    public static string? RemoveLastOccurrence(this string source, string find, StringComparison comparison)
    {
        return source.ReplaceLastOccurrence(find, "", comparison);
    }
        
    public static string? ReplaceLastOccurrence(this string? source, string find, string replaceWith, StringComparison comparison)
    {
        if (source == null)
        {
            return null;
        }
        var place = source.LastIndexOf(find, comparison);
        switch (place)
        {
            case -1: return source;
            default:
                return source.Remove(place, find.Length).Insert(place, replaceWith);
        }
    }

    public static async Task RenderView(this HttpResponse res, IView view, object? model, ActionContext actionContext, ITempDataProvider tempDataProvider, CancellationToken cancellationToken)
    {
        var viewDictionary = actionContext.HttpContext.GetViewData() ?? new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        viewDictionary.Model = model;

        using (var writer = new StringWriter())
        {
            var viewContext = new ViewContext(
                actionContext,
                view,
                viewDictionary,
                new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
                writer,
                new HtmlHelperOptions()
            );

            await view.RenderAsync(viewContext).ConfigureAwait(false);
            res.ContentType = "text/html; charset=UTF-8";
            await res.WriteAsync(writer.ToString(), cancellationToken).ConfigureAwait(false);
        }
    }
        
    public static string? GetViewName(this HttpContext context)
    {
        if (context.Items.TryGetValue("ViewName", out var v) && v is string name)
        {
            return name;
        }
        return null;
    }
        
    public static string? GetActionName(this HttpContext context)
    {
        if (context.GetRouteData().Values.TryGetValue("Action", out var v) && v is string action)
        {
            return action;
        }
        return null;
    }
        
    public static ViewDataDictionary? GetViewData(this HttpContext context)
    {
        if (context.Items.TryGetValue("ViewData", out var value) && value is ViewDataDictionary viewData)
        {
            return viewData;
        }
        return null;
    }
    
    public static void SetViewData(this HttpContext context, ViewDataDictionary viewData)
    {
        context.Items["ViewData"] = viewData;
    }
}