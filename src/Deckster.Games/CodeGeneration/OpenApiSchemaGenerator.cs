using Deckster.Core;
using Deckster.Core.Extensions;
using Deckster.Core.Serialization;
using Deckster.Games.CodeGeneration.Meta;
using Deckster.Games.Reflection;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace Deckster.Games.CodeGeneration;

public class OpenApiSchemaGenerator
{
    private readonly Type _baseType;
    private readonly Dictionary<string, string> _discriminatorMapping = new();
    
    private readonly Dictionary<Type, OpenApiSchema> _types = new();
    
    public Dictionary<string, OpenApiSchema> Schemas { get; } = new();
    
    public OpenApiSchemaGenerator(Type baseType)
    {
        _baseType = baseType;
        var baseSchema = GetSchema(baseType);
        baseSchema.Discriminator = new OpenApiDiscriminator
        {
            PropertyName = "type",
            Mapping = _discriminatorMapping
        };

        var types = baseType.Assembly.GetTypes()
            .Where(t => t.IsClass && t.IsSubclassOf(baseType))
            .OrderByDescending(t => t.IsAbstract)
            .ThenBy(t => InheritanceRelativeTo(t, baseType));
        
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
        if (type.IsAbstract && _baseType.IsAssignableFrom(type))
        {
            schema.Discriminator = new OpenApiDiscriminator
            {
                PropertyName = "type",
                Mapping = new Dictionary<string, string>()
            };
        }
        if (type.IsSubclassOf(_baseType))
        {
            var discriminatorValue = type.GetGameNamespacedName();
            var discriminatorReference = $"#/components/schemas/{discriminatorValue}";
            
            foreach (var parent in type.GetAllBaseTypes().Where(t => t is {IsClass: true, IsAbstract: true} && _baseType.IsAssignableFrom(type)))
            {
                if (_types.TryGetValue(parent, out var parentSchema))
                {
                    parentSchema.Discriminator.Mapping[discriminatorValue] = discriminatorReference;
                }
                else
                {
                    var hest = "pølse";
                }
            }
        }
        
        if (type.IsNullable())
        {
            schema.Nullable = true;
            type = type.GetGenericArguments()[0];
        }

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
                        ExternalResource = $"#/components/schemas/{type.BaseType.GetGameNamespacedName()}"
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