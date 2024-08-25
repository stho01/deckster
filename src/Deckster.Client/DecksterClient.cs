using Deckster.Client.Games.ChatRoom;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Games.Uno;

namespace Deckster.Client;

public class DecksterClient
{
    public GameApi<CrazyEightsClient> CrazyEights { get; }
    public GameApi<ChatRoomClient> ChatRoom { get; }
    public GameApi<UnoClient> Uno { get; }

    public DecksterClient(string url, string token) : this(new Uri(url), token)
    {
        
    }
    
    public DecksterClient(Uri baseUri, string token)
    {
        CrazyEights = new GameApi<CrazyEightsClient>(baseUri.Append("crazyeights"), token, c => new CrazyEightsClient(c));
        ChatRoom = new GameApi<ChatRoomClient>(baseUri.Append("chatroom"), token, c =>
        {
            var client = new ChatRoomClient(c);
            return client;
        });
        Uno = new GameApi<UnoClient>(baseUri.Append("uno"), token, c => new UnoClient(c));
    }
}