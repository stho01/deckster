using Deckster.Client.Common;
using Deckster.Client.Serialization;
using Deckster.Server.Games.TestGame;

namespace Deckster.Server.Games.Common.Meta;

public class GameMeta
{
    public List<MessageMeta> Messages { get; init; } = [];

    public static GameMeta For(Type type)
    {
        var grandParent = typeof(GameHost<,,>);

        var parent = type;
        while (!parent.IsGenericType && parent.BaseType != grandParent)
        {
            parent = parent.BaseType;
            if (parent == null)
            {
                return new GameMeta();
            }
        }
        
        var messages = new List<MessageMeta>
        {
            MessageMeta.For(typeof(FailureResponse))
        };
        foreach (var baseType in parent.GenericTypeArguments)
        {
            if (!baseType.IsAbstract)
            {
                messages.Add(MessageMeta.For(baseType));
            }
            foreach (var subType in baseType.Assembly.GetTypes().Where(t => t.IsSubclassOf(baseType)))
            {
                messages.Add(MessageMeta.For(subType));
            }
        }

        return new GameMeta
        {
            Messages = messages
        };
    }
}

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