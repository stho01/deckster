using Deckster.Client.Protocol;

namespace Deckster.Client.Games.ChatRoom;

public class SendChatMessage : DecksterRequest
{
    public string Message { get; set; }
}

public class ChatMessage : DecksterMessage
{
    public string Sender { get; init; }
    public string Message { get; init; }
}