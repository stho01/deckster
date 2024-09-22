using Deckster.Client.Communication;

namespace Deckster.Client.Games;

public abstract class GameClient : IDisposable, IAsyncDisposable
{
    protected readonly IClientChannel Channel;
    public event Action<GameClient>? Disconnected;

    protected GameClient(IClientChannel channel)
    {
        Channel = channel;
        channel.OnDisconnected += (c, reason) => Disconnected?.Invoke(this);
    }

    public async Task DisconnectAsync()
    {
        await Channel.DisconnectAsync();
    }
    
    public void Dispose()
    {
        Channel.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Channel.DisposeAsync();
    }
}