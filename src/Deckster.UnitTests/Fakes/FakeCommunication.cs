using System.Collections.Concurrent;
using Deckster.Core.Protocol;
using Deckster.Games;
using Deckster.Server.Games;

namespace Deckster.UnitTests.Fakes;

public class FakeCommunication : ICommunication
{
    public List<DecksterNotification> BroadcastNotifications { get; } = [];
    public ConcurrentDictionary<Guid, List<DecksterNotification>> PlayerNotifications { get; } = new();
    public List<DecksterRequest> SelfRequests { get; } = new();
    public ConcurrentDictionary<Guid, List<DecksterResponse>> Responses { get; } = new();

    public FakeCommunication()
    {
        
    }

    public FakeCommunication(GameObject game)
    {
        game.WireUp(this);
    }
    
    public bool HasBroadcasted<TNotification>(Func<TNotification, bool> predicate) => BroadcastNotifications.OfType<TNotification>().Any(predicate);
    public bool HasBroadcasted<TNotification>() => BroadcastNotifications.OfType<TNotification>().Any();

    public bool HasNotifiedEachPlayer<TNotification>()
    {
        return PlayerNotifications.Keys.All(playerId => PlayerNotifications[playerId].OfType<TNotification>().Any());
    }
    
    public bool HasNotifiedEachPlayer<TNotification>(Func<Guid, TNotification, bool> predicate)
    {
        return PlayerNotifications.Keys.All(playerId => PlayerNotifications[playerId].OfType<TNotification>().Any(n => predicate(playerId, n)));
    }
    
    public bool HasNotifiedPlayer<TNotification>(Guid playerId, Func<TNotification, bool> predicate)
    {
        return PlayerNotifications.TryGetValue(playerId, out var notifications) &&
               notifications.OfType<TNotification>().Any(predicate);
    }
    
    public bool HasNotifiedPlayer<TNotification>(Guid playerId)
    {
        return PlayerNotifications.TryGetValue(playerId, out var notifications) &&
               notifications.OfType<TNotification>().Any();
    }
    
    public Task NotifyAllAsync(DecksterNotification notification)
    {
        BroadcastNotifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task RespondAsync(Guid playerId, DecksterResponse response)
    {
        Responses.GetOrAdd(playerId, _ => new List<DecksterResponse>())
            .Add(response);
        return Task.CompletedTask;
    }

    public Task NotifyPlayerAsync(Guid playerId, DecksterNotification notification)
    {
        PlayerNotifications.GetOrAdd(playerId, _ => new List<DecksterNotification>())
            .Add(notification);
        return Task.CompletedTask;
    }    
    public Task NotifySelfAsync(DecksterRequest notification)
    {
        SelfRequests.Add(notification);
        return Task.CompletedTask;
    }
}