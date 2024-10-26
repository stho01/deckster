using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Deckster.Client.Protocol;

namespace Deckster.Client.Serialization;

public static class DecksterJson
{
    public static readonly JsonSerializerOptions Options = Create();
    
    public static readonly JsonSerializerOptions PrettyUnsafe = Create(configure: o =>
    {
        o.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        o.WriteIndented = true;
    });

    public static JsonSerializerOptions Create(Action<DecksterMessageConverterBuilder>? messages = null, Action<JsonSerializerOptions>? configure = null)
    {
      var options = new JsonSerializerOptions
      {
          Converters = {
              new JsonStringEnumConverter(),
          },
          PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
          DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
          AllowTrailingCommas = true, // YES! We are crazy
          ReadCommentHandling = JsonCommentHandling.Skip,
      };

      var builder = new DecksterMessageConverterBuilder().AddAll<DecksterMessage>();
      messages?.Invoke(builder);
      options.Converters.AddRange(builder.GetConverters());
      configure?.Invoke(options);
      return options;
    }

    private static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
    
    public static T? Deserialize<T>(ReadOnlySpan<byte> bytes)
    {
        return JsonSerializer.Deserialize<T>(bytes, Options);
    }

    public static byte[] SerializeToBytes(object item)
    {
        return JsonSerializer.SerializeToUtf8Bytes(item, item.GetType(), Options);
    }

    public static string Pretty(this object? item)
    {
        return item == null ? "null" : JsonSerializer.Serialize(item, item.GetType(), PrettyUnsafe);
    }
}