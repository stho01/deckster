using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Deckster.Client.Protocol;
using Deckster.Server.Games;

namespace Deckster.Server.CodeGeneration.Meta;

internal static class GameTypeExtensions
{
    public static bool InheritsFrom<T>(this Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    public static bool TryGetNotification(this EventInfo e, [MaybeNullWhen(false)] out NotificationMeta meta)
    {
        meta = default;
        var handlerType = e.EventHandlerType;
        if (handlerType == null)
        {
            return false;
        }

        if (handlerType.IsNotifyAll(out var notificationType) || handlerType.IsNotifyPlayer(out notificationType))
        {
            meta = new NotificationMeta
            {
                Name = e.Name,
                Message = MessageMeta.ForType(notificationType)
            };
            return true;
        }

        return false;
    }

    private static bool IsNotifyAll(this Type type, [MaybeNullWhen(false)] out Type argument)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NotifyAll<>))
        {
            argument = type.GenericTypeArguments[0];
            return true;
        }

        argument = default;
        return false;
    }

    private static bool IsNotifyPlayer(this Type type, [MaybeNullWhen(false)] out Type argument)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NotifyPlayer<>))
        {
            argument = type.GenericTypeArguments[0];
            return true;
        }

        argument = default;
        return false;
    }
    
    public static bool TryGetGameMethod(this MethodInfo method, [MaybeNullWhen(false)] out MethodMeta meta)
    {
        if (method.ReturnType.TryGetTaskOfDecksterResponse(out var returnType) &&
            method.TryGetDecksterRequestParameter(out var requestParameter))
        {
            meta = new MethodMeta
            {
                Name = method.Name,
                ReturnType = returnType,
                Parameters = [requestParameter]
            };
            return true;
        }

        meta = default;
        return false;
    }

    private static bool TryGetDecksterRequestParameter(this MethodInfo method, [MaybeNullWhen(false)] out ParameterMeta meta)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 1 &&
            parameters[0].ParameterType.InheritsFrom<DecksterRequest>())
        {
            var parameter = parameters[0];
            meta = new ParameterMeta
            {
                Name = parameter.Name,
                Type = MessageMeta.ForType(parameter.ParameterType)
            };
            return true;
        }

        meta = default;
        return false;
    }

    private static bool TryGetTaskOfDecksterResponse(this Type type, [MaybeNullWhen(false)] out MessageMeta meta)
    {
        meta = default;
        if (!type.IsGenericType)
        {
            return false;
        }
        
        var genericTypeDefinition = type.GetGenericTypeDefinition();
        if (type.IsGenericType &&
            type.BaseType == typeof(Task) &&
            genericTypeDefinition == typeof(Task<>) &&
            type.GenericTypeArguments[0].InheritsFrom<DecksterResponse>())
        {
            meta = MessageMeta.ForType(type.GenericTypeArguments[0]);
            return true;
        }

        meta = default;
        return false;

    }
}