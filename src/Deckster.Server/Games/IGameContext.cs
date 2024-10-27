using Deckster.Client.Protocol;

namespace Deckster.Server.Games;

public interface ICommunication
{
    Task NotifyAllAsync(DecksterNotification notification, CancellationToken cancellationToken = default);
    Task RespondAsync(Guid playerId, DecksterResponse response, CancellationToken cancellationToken = default);
    Task NotifyAsync(Guid playerId, DecksterNotification notification, CancellationToken cancellationToken = default);
}