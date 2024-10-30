using Deckster.Client.Serialization;

namespace Deckster.Server.CodeGeneration.Meta;

public class MessageMeta
{
    public string Name { get; init; }
    public string Type { get; init; }

    public override string ToString()
    {
        return Name;
    }

    public static MessageMeta ForType(Type type)
    {
        return new MessageMeta
        {
            Name = type.Name,
            Type = type.GetGameNamespacedName()
        };
    }
}