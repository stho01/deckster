using System.Net.WebSockets;
using Deckster.Core.Games.Common;

namespace Deckster.Server.Games;

public class ConnectingPlayer
{
    public TaskCompletionSource TaskCompletionSource { get; } = new();
    public Guid ConnectionId { get; } = Guid.NewGuid();
    public PlayerData Player { get; }
    public WebSocket ActionSocket { get; }
    public IGameHost GameHost { get; }
    
    public ConnectingPlayer(PlayerData player, WebSocket actionSocket, IGameHost host)
    {
        Player = player;
        ActionSocket = actionSocket;
        GameHost = host;
    }

    public async Task CancelAsync()
    {
        TaskCompletionSource.SetResult();
        await ActionSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", default);
        ActionSocket.Dispose();
    }
}