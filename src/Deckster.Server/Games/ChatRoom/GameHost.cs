using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Deckster.Client.Common;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.ChatRoom;

public abstract class GameHost<TRequest, TResponse, TNotification> : IGameHost
    where TRequest : DecksterMessage
    where TResponse : DecksterMessage
    where TNotification : DecksterMessage

{
    public event EventHandler<IGameHost>? OnEnded;
    public abstract string GameType { get; }
    public string Name { get; init; }
    public abstract GameState State { get; }

    protected readonly JsonSerializerOptions JsonOptions = DecksterJson.Create(o =>
    {
        o.AddAll<TRequest>().AddAll<TResponse>().AddAll<TNotification>();
    });

    public abstract Task Start();

    public abstract bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error);
    public abstract Task CancelAsync();
    public abstract ICollection<PlayerData> GetPlayers();
}

