using Deckster.Client.Protocol;

namespace Deckster.Server.Games;

public class NullContext : ICommunicationContext
{
    public static NullContext Instance { get; } = new();

    private NullContext()
    {
        
    }
    
    public Task NotifyAllAsync(DecksterNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RespondAsync(Guid playerId, DecksterResponse response, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task NotifyAsync(Guid playerId, DecksterNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}