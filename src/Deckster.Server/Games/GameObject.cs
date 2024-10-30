using System.Reflection;
using System.Text.Json.Serialization;
using Deckster.Client.Protocol;
using Deckster.Server.Data;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games;

public delegate Task NotifyAll<in T>(T notification) where T : DecksterNotification;
public delegate Task NotifyPlayer<in T>(Guid playerId, T notification) where T : DecksterNotification;

public abstract class GameObject : DatabaseObject
{
    [JsonIgnore]
    public Func<Guid, DecksterResponse, Task> RespondAsync { get; set; } = (_, _) => Task.CompletedTask;
    
    public DateTimeOffset StartedTime { get; init; }
    public abstract GameState State { get; }

    // ReSharper disable once UnusedMember.Global
    // Used by Marten
    public int Version { get; set; }

    public abstract Task StartAsync();

    public void WireUp(ICommunication communication)
    {
        RespondAsync = communication.RespondAsync;
        
        foreach (var e in GetType().GetEvents())
        {
            var handlerType = e.EventHandlerType;
            if (handlerType == null)
            {
                continue;
            }
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyAll<>))
            {
                e.AddEventHandler(this, GetDelegate(communication.NotifyAllAsync, handlerType));
            }
            
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyPlayer<>))
            {
                e.AddEventHandler(this, GetDelegate(communication.NotifyPlayerAsync, handlerType));
            }
        }

        foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var handlerType = property.PropertyType;
            
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyAll<>))
            {
                property.SetValue(this, GetDelegate(communication.NotifyAllAsync, handlerType));
            }
            
            if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(NotifyPlayer<>))
            {
                property.SetValue(this, GetDelegate(communication.NotifyPlayerAsync, handlerType));
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
