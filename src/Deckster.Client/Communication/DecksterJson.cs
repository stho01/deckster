using System.Text.Json;
using System.Text.Json.Serialization;
using Deckster.Client.Common;

namespace Deckster.Client.Communication;

public static class DecksterJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(),
            new DerivedTypeConverter<CommandResult>()
        },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static T? Deserialize<T>(byte[] bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes, Options);
    }
}