using Deckster.Core.Protocol;

namespace Deckster.Core.Games.ChatRoom;

public class SendChatRequest : DecksterRequest
{
    public string Message { get; set; }
}

public class ChatNotification : DecksterNotification
{
    public string Sender { get; init; }
    public string Message { get; init; }
}

public class ChatResponse : DecksterResponse
{
    
}