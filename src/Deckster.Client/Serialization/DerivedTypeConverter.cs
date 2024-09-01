using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Deckster.Client.Protocol;

namespace Deckster.Client.Serialization;

internal class DerivedTypeConverter<T> : JsonConverter<T> where T : IHaveDiscriminator
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Dictionary<string, Type> TypeMap;

    static DerivedTypeConverter()
    {
        var types = from t in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
            where t.IsClass &&
                  !t.IsAbstract &&
                  typeof(T).IsAssignableFrom(t)
            select t;
        TypeMap = types.ToDictionary(t => t.GetGameNamespacedName(), t => t, StringComparer.OrdinalIgnoreCase); // We don't care about casing, do we? Nah..
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(ref reader);
        var discriminator = node?["type"]?.GetValue<string>();
        if (discriminator == null)
        {
            return default;
        }

        if (TypeMap.TryGetValue(discriminator, out var type))
        {
            return (T?) node.Deserialize(type, options);
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
}