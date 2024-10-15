using Deckster.Server.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Deckster.Server.Controllers;

public class OpenApiResult : IActionResult
{
    private readonly OpenApiDocument _document;

    public OpenApiResult(OpenApiDocument document)
    {
        _document = document;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var request = context.HttpContext.Request; 
        var response = context.HttpContext.Response;
        var (format, contentType) = request.AcceptsYaml()
            ? (OpenApiFormat.Yaml, "text/yaml")
            : (OpenApiFormat.Json, "application/json");
        
        using var stream = new MemoryStream();
        _document.Serialize(stream, OpenApiSpecVersion.OpenApi3_0, format);
            
        stream.Position = 0;
        response.ContentType = contentType;
        await stream.CopyToAsync(response.Body);
    }
}