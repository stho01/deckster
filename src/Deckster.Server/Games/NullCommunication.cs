using Deckster.Client.Protocol;

namespace Deckster.Server.Games;

public class NullCommunication : ICommunication
{
    public static NullCommunication Instance { get; } = new();

    private NullCommunication()
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