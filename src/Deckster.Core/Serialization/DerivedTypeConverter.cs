using System.Text.Json;
using System.Text.Json.Serialization;
using Deckster.Core.Protocol;

namespace Deckster.Core.Serialization;

public class DerivedTypeConverter<T> : JsonConverter<T> where T : DecksterMessage
{
    private readonly Dictionary<string, Type> _typeMap;

    public DerivedTypeConverter(Dictionary<string, Type> typeMap)
    {
        _typeMap = typeMap;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var element = JsonElement.ParseValue(ref reader);
        if (!element.TryGetProperty("type", out var p) || p.ValueKind != JsonValueKind.String)
        {
            return default;
        }

        var discriminator = p.GetString();
        if (discriminator == null)
        {
            return default;
        }

        if (_typeMap.TryGetValue(discriminator, out var type))
        {
            return (T?) element.Deserialize(type, options);
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize<object>(writer, value, options);
    }
}

public static class TypeExtensions
{
    /// <summary>
    /// Last part
    /// </summary>
    /// <returns>[last part of namespace].[type name], e.g Uno.DrawCardRequest</returns>
    public static string GetGameNamespacedName(this Type type)
    {
        if (type.IsNullable(out var inner))
        {
            type = inner;
        }
        if (type.FullName == null)
        {
            return type.Name;
        }

        var periodCount = 0;
        var start = 0;
        for (var ii = type.FullName.Length - 1; ii >= 0; ii--)
        {
            switch (type.FullName[ii])
            {
                case '.':
                    if (periodCount > 0)
                    {
                        return type.FullName[start..];
                    }
                    periodCount++;
                    break;
            }
            start = ii;
        }
        return type.FullName[start..];
    }

    public static bool IsNullable(this Type type, out Type inner)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            inner = type.GenericTypeArguments[0];
            return true;
        }

        inner = default;
        return false;
    }
}