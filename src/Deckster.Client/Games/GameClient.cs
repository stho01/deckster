using Deckster.Client.Communication;

namespace Deckster.Client.Games;

public abstract class GameClient
{
    protected readonly IClientChannel _channel;
    public event Action<GameClient>? Disconnected;

    protected GameClient(IClientChannel channel)
    {
        _channel = channel;
        channel.OnDisconnected += (c, reason) => Disconnected?.Invoke(this);
    }

    public async Task DisconnectAsync()
    {
        await _channel.DisconnectAsync();
    }
}