using Deckster.Client.Serialization;
using Deckster.Client.Sugar;
using Deckster.Server.CodeGeneration.Meta;
using Deckster.Server.Reflection;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Deckster.Server.CodeGeneration;

public class OpenApiSchemaGenerator
{
    private readonly Dictionary<Type, OpenApiSchema> _types = new();
    
    public Dictionary<string, OpenApiSchema> Schemas { get; } = new();

    private static int InheritanceRelativeTo(Type type, Type baseType)
    {
        if (type == baseType)
        {
            return 0;
        }

        if (type.BaseType == null)
        {
            return -1;
        }

        var level = 0;
        var t = type;
        while (t.BaseType != null)
        {
            t = t.BaseType;
            level++;
        }

        return level;
    }
    
    public OpenApiSchemaGenerator(Type baseType)
    {
        var baseSchema = GetSchema(baseType);
        baseSchema.Discriminator = new OpenApiDiscriminator
        {
            PropertyName = "type"
        };
        
        var types = from t in baseType.Assembly.GetTypes()
            where t.IsClass && t.IsSubclassOf(baseType)
            orderby InheritanceRelativeTo(t, baseType)
            select t;
        
        foreach (var type in types)
        {
            if (Schemas.ContainsKey(type.Name))
            {
                continue;
            }
            if (_types.TryGetValue(type, out var schema))
            {
                Schemas[type.GetGameNamespacedName()] = schema;
            }
            else
            {
                _ = GetSchema(type);    
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

        Schemas.TryAdd(type.GetGameNamespacedName(), schema);
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
                            ExternalResource = $"#/components/schemas/{elementType.GetGameNamespacedName()}"
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
        Schemas[type.GetGameNamespacedName()] = schema;

        if (type.BaseType != null && _types.TryGetValue(type.BaseType, out var baseSchema))
        {
            schema.AllOf = new List<OpenApiSchema>
            {
                new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        ExternalResource = $"#/components/schema/{type.BaseType.GetGameNamespacedName()}"
                    }
                },
                new OpenApiSchema
                {
                    Type = "object",
                    Properties = type.GetOwnProperties().ToDictionary(p => p.Name.ToCamelCase(), p => GetSchema(p.PropertyType)) 
                }
            };
        }
        else
        {
            schema.Properties = type.GetProperties().ToDictionary(p => p.Name.ToCamelCase(), p => GetSchema(p.PropertyType));    
        }
        
         
        return schema;
    }
}