using Deckster.Client.Protocol;

namespace Deckster.Client.Games.ChatRoom;

public abstract class ChatRequest : DecksterRequest;

public class SendChatMessage : ChatRequest
{
    public string Message { get; set; }
}

public class ChatNotification : DecksterNotification
{
    public string Sender { get; init; }
    public string Message { get; init; }
}

public class ChatResponse : DecksterResponse;