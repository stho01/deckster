using Deckster.Core.Protocol;

namespace Deckster.Server.Games;

public interface ICommunication
{
    Task NotifyAllAsync(DecksterNotification notification);
    Task RespondAsync(Guid playerId, DecksterResponse response);
    Task NotifyPlayerAsync(Guid playerId, DecksterNotification notification);
    Task NotifySelfAsync(DecksterRequest notification);
}