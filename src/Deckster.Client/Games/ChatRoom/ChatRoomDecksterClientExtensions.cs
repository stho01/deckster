namespace Deckster.Client.Games.ChatRoom;

public static class ChatRoomDecksterClientExtensions
{
    public static GameApi<ChatRoomClient> ChatRoom(this DecksterClient client)
    {
        return new GameApi<ChatRoomClient>(client.BaseUri.Append("chatroom"), client.Token, c => new ChatRoomClient(c));
    }
}