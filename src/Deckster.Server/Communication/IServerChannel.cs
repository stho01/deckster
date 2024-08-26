using Deckster.Client.Common;
using Deckster.Client.Protocol;

namespace Deckster.Server.Communication;

public interface IServerChannel : IDisposable
{
    event Action<PlayerData, DecksterRequest>? Received;
    event Action<IServerChannel> Disconnected;
    
    PlayerData Player { get; }
    ValueTask ReplyAsync(DecksterResponse response, CancellationToken cancellationToken = default);
    ValueTask PostMessageAsync(DecksterMessage message, CancellationToken cancellationToken = default);
    Task WeAreDoneHereAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    
    void Start(CancellationToken cancellationToken);
}