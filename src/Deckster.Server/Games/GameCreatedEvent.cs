namespace Deckster.Server.Games;

public abstract class GameCreatedEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset StartedTime { get; init; } = DateTimeOffset.UtcNow;
    public int InitialSeed { get; init; } = new Random().Next(0, int.MaxValue);
}
