using System.Reflection;
using Deckster.Games;

namespace Deckster.Server.Games;

public static class GameObjectExtensions
{
    public static void WireUp(this GameObject o, ICommunication communication)
    {
        o.RespondAsync = communication.RespondAsync;
        
        foreach (var e in o.GetType().GetEvents())
        {
            var handlerType = e.EventHandlerType;
            if (handlerType == null)
            {
                continue;
            }
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyAll<>))
            {
                e.AddEventHandler(o, GetDelegate(communication.NotifyAllAsync, handlerType));
            }
            
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyPlayer<>))
            {
                e.AddEventHandler(o, GetDelegate(communication.NotifyPlayerAsync, handlerType));
            }
        }

        foreach (var property in o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var handlerType = property.PropertyType;
            
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyAll<>))
            {
                property.SetValue(o, GetDelegate(communication.NotifyAllAsync, handlerType));
            }
            
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyPlayer<>))
            {
                property.SetValue(o, GetDelegate(communication.NotifyPlayerAsync, handlerType));
            }
        }
    }

    private static Delegate? GetDelegate(Delegate del, Type handlerType)
    {
        if (del.GetType() == handlerType)
        {
            return del;
        }
        
        var invList = del.GetInvocationList();
        for (var ii = 0; ii < invList.Length; ii++)
        {
            if (invList[ii].GetType() != handlerType)
            {
                invList[ii] = Delegate.CreateDelegate(handlerType, invList[ii].Target, invList[ii].Method);
            }
        }

        return Delegate.Combine(invList);
    }
}