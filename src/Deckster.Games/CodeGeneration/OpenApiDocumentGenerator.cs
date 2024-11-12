using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Deckster.Games.CodeGeneration;

public class OpenApiDocumentGenerator
{
    private readonly OpenApiDocument _document;
    
    public OpenApiDocumentGenerator(Type baseType)
    {
        var schemaGenerator = new OpenApiSchemaGenerator(baseType);
        _document = new OpenApiDocument
        {
            Info = new OpenApiInfo
            {
                Title = "Deckster",
                Description = "Deckster",
                Version = "3.141593"
            },
            Components = new OpenApiComponents
            {
                Schemas = schemaGenerator.Schemas
            }
        };
    }

    public Task WriteAsYamlAsync(FileInfo file) => WriteAsync(file, OpenApiFormat.Yaml);
    public Task WriteAsJsonAsync(FileInfo file) => WriteAsync(file, OpenApiFormat.Json);

    private async Task WriteAsync(FileInfo file, OpenApiFormat format)
    {
        using var stream = new MemoryStream();
        _document.Serialize(stream, OpenApiSpecVersion.OpenApi3_0, format);
        stream.Position = 0;

        if (file.Exists)
        {
            file.Delete();
        }
        await using var fileStream = file.Open(FileMode.CreateNew);
        await stream.CopyToAsync(fileStream);
        await fileStream.FlushAsync();
    }
}