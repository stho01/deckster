using Deckster.Client.Protocol;

namespace Deckster.Server.Games;

public class NullCommunication : ICommunication
{
    public static NullCommunication Instance { get; } = new();

    private NullCommunication()
    {
        
    }
    
    public Task NotifyAllAsync(DecksterNotification notification)
    {
        return Task.CompletedTask;
    }

    public Task RespondAsync(Guid playerId, DecksterResponse response)
    {
        return Task.CompletedTask;
    }

    public Task NotifyPlayerAsync(Guid playerId, DecksterNotification notification)
    {
        return Task.CompletedTask;
    }
}