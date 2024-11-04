using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Deckster.Games;
using Deckster.Games.CodeGeneration.Meta;

namespace Deckster.CodeGenerator.CSharp;

public class CSharpGameMeta
{
    public string Name { get; }
    public NotificationInfo[] Notifications { get; }
    public GameMethodInfo[] Methods { get; }
    public GameExtensionMethodInfo[] ExtensionMethods { get; }
    public string[] Usings { get; }
    
    public CSharpGameMeta(Type gameType)
    {
        Name = gameType.Name.Replace("Game", "");
        Notifications = gameType.GetNotifications().ToArray();
        Methods = gameType.GetGameMethods().ToArray();

        var usings = new HashSet<string>();
        foreach (var ns in Notifications.Select(n => n.MessageType.Namespace))
        {
            usings.AddIfNotNull(ns);
        }

        var extensionMethods = new List<GameExtensionMethodInfo>();
        foreach (var method in Methods)
        {
            if (method.Request.ParameterType.Namespace != null)
            {
                usings.Add(method.Request.ParameterType.Namespace);
            }

            if (method.ResponseType.Namespace != null)
            {
                usings.Add(method.ResponseType.Namespace);
            }

            if (method.TryGetExtensionMethod(out var extensionMethod))
            {
                extensionMethods.Add(extensionMethod);
                usings.AddIfNotNull(extensionMethod.ReturnType.Namespace);
                usings.AddRangeIfNotNull(extensionMethod.Parameters.Select(p => p.ParameterType.Namespace));
            }
        }

        ExtensionMethods = extensionMethods.ToArray();

        Usings = usings.ToArray();
    }

    public static bool TryGetFor(Type type, [MaybeNullWhen(false)] out CSharpGameMeta meta)
    {
        if (type.InheritsFrom<GameObject>() && !type.IsAbstract)
        {
            meta = new CSharpGameMeta(type);
            return true;
        }

        meta = default;
        return false;
    }
}

public record NotificationInfo(string Name, Type MessageType);

public record GameMethodInfo(string Name, ParameterInfo Request, Type ResponseType);

public class GameExtensionMethodInfo(string Name, GameParameterInfo[] Parameters, GameMethodInfo Method, Type ReturnType, GameParameterInfo[]? ReturnParameters)
{
    public string Name { get; init; } = Name;
    public GameParameterInfo[] Parameters { get; init; } = Parameters;
    public GameMethodInfo Method { get; init; } = Method;
    public Type ReturnType { get; init; } = ReturnType;
    public GameParameterInfo[]? ReturnParameters { get; init; } = ReturnParameters;

    public void Deconstruct(out string Name, out GameParameterInfo[] Parameters, out GameMethodInfo Method, out Type ReturnType, out GameParameterInfo[]? ReturnParameters)
    {
        Name = this.Name;
        Parameters = this.Parameters;
        Method = this.Method;
        ReturnType = this.ReturnType;
        ReturnParameters = this.ReturnParameters;
    }
}

public record GameParameterInfo(string Name, Type ParameterType);