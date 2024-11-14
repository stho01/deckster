using Deckster.Server.Middleware;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Deckster.Server.ContentNegotiation.Xml;

public class XmlOutputFormatter : IOutputFormatter
{
    public bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        var canWrite = context.HttpContext.Request.Accepts("xml");
        return canWrite;
    }

    public Task WriteAsync(OutputFormatterWriteContext context)
    {
        var response = context.HttpContext.Response;
        response.ContentType = "text/xml;charset=utf-8";
        response.StatusCode = 400;
        return response.WriteAsync("<?xml version=\"1.0\"?><Seriously><Question>You must be kidding, right?</Question></Seriously>");
    }
}