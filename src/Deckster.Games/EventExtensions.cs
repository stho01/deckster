using Deckster.Core.Protocol;

namespace Deckster.Games;

public static class EventExtensions
{
    public static Task InvokeOrDefault<T>(this NotifyAll<T>? handler, T notification) where T : DecksterNotification
    {
        return handler?.Invoke(notification) ?? Task.CompletedTask;
    }
    
    public static Task InvokeOrDefault<T>(this NotifyAll<T>? handler, Func<T> notification) where T : DecksterNotification
    {
        return handler?.Invoke(notification()) ?? Task.CompletedTask;
    }
    
    public static Task InvokeOrDefault<T>(this NotifyPlayer<T>? handler, Guid id, T notification) where T : DecksterNotification
    {
        return handler?.Invoke(id, notification) ?? Task.CompletedTask;
    }
    
    public static Task InvokeOrDefault<T>(this NotifyPlayer<T>? handler, Guid id, Func<T> notification) where T : DecksterNotification
    {
        return handler?.Invoke(id, notification()) ?? Task.CompletedTask;
    }
}