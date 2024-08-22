using Deckster.Client.Communication;

namespace Deckster.Client.Games;

public abstract class GameClient
{
    protected readonly IClientChannel _channel;
    public event Action<GameClient>? Disconnected;

    protected GameClient(IClientChannel channel)
    {
        _channel = channel;
        channel.OnDisconnected += c => Disconnected?.Invoke(this);
    }

    public async Task DisconnectAsync(bool normal, string reason)
    {
        await _channel.DisconnectAsync(normal, reason);
    }
}