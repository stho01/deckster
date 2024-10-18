using System.Reflection;
using Deckster.Client.Serialization;

namespace Deckster.Server.CodeGeneration.Meta;

public class MessageMeta
{
    public string Type { get; init; }
    public PropertyMeta[]? Properties { get; init; }

    public static MessageMeta For(Type type)
    {
        var properties = type.GetProperties().Where(p => p.Name != "Type").Select(PropertyMeta.For).ToArray();
        return new MessageMeta
        {
            Type = type.GetGameNamespacedName(),
            Properties = properties
        };
    }
}

public class PropertyMeta
{
    public string Name { get; init; }
    public string Type { get; init; }

    public static PropertyMeta For(PropertyInfo property)
    {
        return new PropertyMeta
        {
            Name = property.Name,
            Type = property.PropertyType.GetFriendlyName()
        };
    }
}