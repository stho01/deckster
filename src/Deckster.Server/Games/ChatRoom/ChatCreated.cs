namespace Deckster.Server.Games.ChatRoom;

public class ChatCreatedEvent : GameCreatedEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset StartedTime { get; init; } = DateTimeOffset.UtcNow;
}