using Deckster.Client.Protocol;
using Deckster.Server.CodeGeneration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

namespace Deckster.Server.Controllers;

[Route("meta")]
public class MetaController : ControllerBase
{
    [HttpGet("messages")]
    public object GetOpenApi()
    {
        var builder = new OpenApiSchemaGenerator(typeof(DecksterMessage));
        
        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = builder.Schemas
            }
        };

        return new OpenApiResult(document);
    }
}