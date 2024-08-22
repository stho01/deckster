using Deckster.Client.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Communication;

public interface IClientChannel : IDisposable, IAsyncDisposable
{
    PlayerData PlayerData { get; }
    event Action<IClientChannel, DecksterMessage>? OnMessage;
    event Action<IClientChannel>? OnDisconnected;
    Task DisconnectAsync(bool normal, string reason, CancellationToken cancellationToken = default);
    Task<DecksterResponse> SendAsync(DecksterRequest message, CancellationToken cancellationToken = default);
}

public static class ClientChannelExtensions
{
    public static async Task<TResult> GetAsync<TResult>(this IClientChannel channel, DecksterRequest request,
        CancellationToken cancellationToken = default)
        where TResult : DecksterResponse
    {
        var result = await channel.SendAsync(request, cancellationToken);
        return result switch
        {
            null => throw new Exception("Result is null. Wat"),
            FailureResponse r => throw new Exception(r.Message),
            TResult r => r,
            _ => throw new Exception($"Unknown result '{result.GetType().Name}'")
        };
    }
}