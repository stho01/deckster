using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Deckster.Core;
using Deckster.Core.Extensions;
using Deckster.Core.Protocol;
using Deckster.Games.CodeGeneration.Meta;

namespace Deckster.CodeGenerator.CSharp;

public static class GameReflectionExtensions
{
    public static IEnumerable<NotificationInfo> GetNotifications(this Type type)
    {
        foreach (var e in type.GetEvents())
        {
            if (e.IsNotifyAll(out var argument) || e.IsNotifyPlayer(out argument))
            {
                yield return new NotificationInfo(e.Name, argument);
            }
        }
    }

    public static IEnumerable<GameMethodInfo> GetGameMethods(this Type type)
    {
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(m => !m.IsSpecialName))
        {
            if (method.IsGameMethod(out var info))
            {
                yield return info;
            }
        }
    }

    public static bool TryGetExtensionMethod(this GameMethodInfo method, [MaybeNullWhen(false)] out GameExtensionMethodInfo extension)
    {
        if (method.Request.ParameterType.TryDecomposeToParameters(out var parameters))
        {
            if (method.ResponseType.TryDecomposeToParameters(out var returnParameters))
            {
                extension = new GameExtensionMethodInfo(method.Name, parameters, method, method.ResponseType, returnParameters);
                return true;
            }
            
            extension = new GameExtensionMethodInfo(method.Name, parameters, method, method.ResponseType, null);
            return true;
        }

        extension = default;
        return false;
    }

    private static bool TryDecomposeToParameters(this Type type, [MaybeNullWhen(false)] out GameParameterInfo[] parameters)
    {
        if (type.InheritsFrom<DecksterRequest>())
        {
            var properties = type.GetProperties()
                .Where(p => p.GetSetMethod() != null &&
                            p.Name != nameof(DecksterRequest.PlayerId) &&
                            p.Name != nameof(DecksterMessage.Type));
            parameters = properties.Select(p => new GameParameterInfo(p.Name.ToCamelCase(), p.PropertyType)).ToArray();
            return true;    
        }

        if (type.InheritsFrom<DecksterResponse>())
        {
            var properties = type.GetProperties()
                .Where(p => p.Name != nameof(DecksterResponse.Error) &&
                            p.Name != nameof(DecksterResponse.HasError) &&
                            p.Name != nameof(DecksterMessage.Type));
            parameters = properties.Select(p => new GameParameterInfo(p.Name.ToCamelCase(), p.PropertyType)).ToArray();
            return true;
        }

        parameters = default;
        return false;

    }

    private static bool IsGameMethod(this MethodInfo method, [MaybeNullWhen(false)] out GameMethodInfo gameMethod)
    {
        if (method.DeclaringType != null &&
            method.TryGetRequestParameter(out var parameter) &&
            method.ReturnType.IsTaskOfDecksterResponse(out var responseType))
        {
            gameMethod = new GameMethodInfo(method.Name.WithoutAsync(), parameter, responseType);
            return true;
        }

        gameMethod = default;
        return false;
    }

    public static string WithoutAsync(this string name) => name.EndsWith("Async") ? name.Replace("Async", "") : name;

    private static bool TryGetRequestParameter(this MethodInfo info, [MaybeNullWhen(false)] out ParameterInfo parameter)
    {
        var parameters = info.GetParameters();
        if (parameters.Length == 1 && parameters[0].ParameterType.InheritsFrom<DecksterRequest>())
        {
            parameter = parameters[0];
            return true;
        }

        parameter = default;
        return false;
    }

    private static bool IsTaskOfDecksterResponse(this Type type, [MaybeNullWhen(false)] out Type responseType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>) && type.GenericTypeArguments[0].InheritsFrom<DecksterResponse>())
        {
            responseType = type.GenericTypeArguments[0];
            return true;
        }

        responseType = default;
        return false;
    }
}