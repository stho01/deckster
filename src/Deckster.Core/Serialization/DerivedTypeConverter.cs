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