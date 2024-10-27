using Deckster.Client.Games.ChatRoom;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.ChatRoom;

public class Chat : GameObject
{
    public override GameState State => GameState.Running;

    public List<SendChatRequest> Transcript { get; init; } = [];

    public static Chat Create(ChatCreatedEvent e)
    {
        return new Chat
        {
            Id = e.Id,
            StartedTime = e.StartedTime,
        };
    }
    
    public async Task ChatAsync(SendChatRequest @event)
    {
        Transcript.Add(@event);
        await Communication.RespondAsync(@event.PlayerId, new ChatResponse());
        await Communication.NotifyAllAsync(new ChatNotification
        {
            Sender = @event.PlayerId.ToString(),
            Message = @event.Message
        });
    }

    public override Task StartAsync()
    {
        return Task.CompletedTask;
    }
}

