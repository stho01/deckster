using Deckster.Client.Serialization;

namespace Deckster.Server.Games.Common.Meta;

public class MessageMeta
{
    public string Type { get; init; }

    public static MessageMeta For(Type type)
    {
        return new MessageMeta
        {
            Type = type.GetGameNamespacedName()
        };
    }
}