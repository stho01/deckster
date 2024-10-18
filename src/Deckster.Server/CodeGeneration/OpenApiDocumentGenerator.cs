using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Deckster.Server.CodeGeneration;

public class OpenApiDocumentGenerator
{
    private readonly OpenApiDocument _document;
    
    public OpenApiDocumentGenerator(Type baseType)
    {
        var schemaGenerator = new OpenApiSchemaGenerator(baseType);
        _document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = schemaGenerator.Schemas
            }
        };
    }

    public Task WriteAsYamlAsync(string path) => WriteAsync(path, OpenApiFormat.Yaml);
    public Task WriteAsJsonAsync(string path) => WriteAsync(path, OpenApiFormat.Json);

    private async Task WriteAsync(string path, OpenApiFormat format)
    {
        using var stream = new MemoryStream();
        _document.Serialize(stream, OpenApiSpecVersion.OpenApi3_0, format);
        stream.Position = 0;
        
        await using var fileStream = File.Exists(path) ? File.Open(path, FileMode.Truncate) : File.Open(path, FileMode.CreateNew);
        await stream.CopyToAsync(fileStream);
        await fileStream.FlushAsync();
    }
}