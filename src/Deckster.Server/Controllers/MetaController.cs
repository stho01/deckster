using Deckster.Client.Protocol;
using Deckster.Client.Sugar;
using Deckster.Server.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Deckster.Server.Controllers;

[Route("meta")]
public class MetaController : ControllerBase
{
    [HttpGet("messages")]
    public object GetOpenApi()
    {
        var baseType = typeof(DecksterMessage);
        var types = from t in baseType.Assembly.GetTypes()
            where t.IsClass && baseType.IsAssignableFrom(t)
            select t;
        var builder = new OpenApiSchemaGenerator(types);
        
        var document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                Schemas = builder.Schemas
            }
        };

        return Request.Accepts("text/yaml")
            ? document.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0)
            : document.SerializeAsJson(OpenApiSpecVersion.OpenApi3_0);

    }
}

public class OpenApiSchemaGenerator
{
    private readonly Dictionary<Type, OpenApiSchema> _types = new();
    
    public Dictionary<string, OpenApiSchema> Schemas { get; } = new();

    public OpenApiSchemaGenerator(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            if (Schemas.ContainsKey(type.Name))
            {
                continue;
            }
            if (_types.TryGetValue(type, out var schema))
            {
                Schemas[type.Name] = schema;
            }
            else
            {
                Schemas[type.Name] = GetSchema(type);    
            }
        }
    }

    private OpenApiSchema GetSchema(Type type)
    {
        if (_types.TryGetValue(type, out var schema))
        {
            return schema;
        }
        if (type.IsSimpleType())
        {
            schema = type.MapTypeToOpenApiPrimitiveType();
            _types[type] = schema;
            return schema;
        }

        if (type.IsCollectionType())
        {
            schema = ToCollection(type);
            _types[type] = schema;
            return schema;
        }

        schema = ToComplex(type);

        Schemas.TryAdd(type.Name, schema);
        _types[type] = schema;
        return schema;
    }

    private OpenApiSchema ToCollection(Type type)
    {
        var elementType = type.GetCollectionElementType();
        if (elementType != null)
        {
            if (elementType.IsSimpleType())
            {
                var schema = new OpenApiSchema
                {
                    Type = "array",
                    Items = GetSchema(elementType)
                };
                return schema;
            }
            else
            {
                _ = GetSchema(elementType);
            
                var schema = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Reference = new OpenApiReference
                        {
                            ExternalResource = $"#/components/schemas/{elementType.Name}"
                        }
                    }
                };
            
                _types[type] = schema;
                return schema;    
            }
        }
        else
        {
            var schema = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema
                {
                    Type = "object"
                }
            };
            
            _types[type] = schema;
            return schema;
        }
    }
    
    private OpenApiSchema ToComplex(Type type)
    {
        var schema = new OpenApiSchema
        {
            Type = "object"
        };

        _types[type] = schema;
        Schemas[type.Name] = schema;

        schema.Properties = type.GetProperties().ToDictionary(p => p.Name.ToCamelCase(), p => GetSchema(p.PropertyType)); 
        return schema;
    }
}