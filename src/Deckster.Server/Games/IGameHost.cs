using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Common;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games;

public interface IGameHost
{
    public event Action<IGameHost>? OnEnded;
    string GameType { get; }
    string Name { get; set; }
    GameState State { get; }
    Task StartAsync();
    bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error);
    bool TryAddBot([MaybeNullWhen(true)] out string error);
    Task EndAsync();
    ICollection<PlayerData> GetPlayers();
}