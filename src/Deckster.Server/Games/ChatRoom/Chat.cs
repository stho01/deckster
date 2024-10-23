using Deckster.Client.Games.ChatRoom;

namespace Deckster.Server.Games.ChatRoom;

public class Chat : GameObject
{
    private ICommunicationContext _context;
    public List<SendChatMessage> Transcript { get; init; } = [];

    public static Chat Create(ChatCreatedEvent e)
    {
        return new Chat
        {
            Id = e.Id,
            StartedTime = e.StartedTime,
            _context = e.GetContext()
        };
    }
    
    public async Task HandleAsync(SendChatMessage @event)
    {
        await Apply(@event);
        await _context.RespondAsync(@event.PlayerId, new ChatResponse());
        await _context.NotifyAllAsync(new ChatNotification
        {
            Sender = @event.PlayerId.ToString(),
            Message = @event.Message
        });
    }
    
    public Task Apply(SendChatMessage @event)
    {
        Transcript.Add(@event);
        return Task.CompletedTask;
    }
}

