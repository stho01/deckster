namespace Deckster.Server.Games;

public abstract class GameCreatedEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset StartedTime { get; init; } = DateTimeOffset.UtcNow;
    public int InitialSeed { get; init; } = new Random().Next(0, int.MaxValue);
    
    private ICommunicationContext _context = NullContext.Instance;
    
    public void SetContext(ICommunicationContext context) => _context = context;
    public ICommunicationContext GetContext()
    {
        var context = _context;
        _context = NullContext.Instance;
        return context;
    }
}

public static class GameCreatedEventExtensions
{
    public static T WithCommunicationContext<T>(this T e, ICommunicationContext context) where T : GameCreatedEvent
    {
        e.SetContext(context);
        return e;
    }
}