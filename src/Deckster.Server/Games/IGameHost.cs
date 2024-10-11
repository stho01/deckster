using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games;

public interface IGameHost
{
    string GameType { get; }
    string Name { get; init; }
    GameState State { get; }
    Task Start();
    bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error);
    Task CancelAsync();
    ICollection<PlayerData> GetPlayers();
}