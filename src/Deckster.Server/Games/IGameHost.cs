using System.Diagnostics.CodeAnalysis;
using Deckster.Server.Communication;

namespace Deckster.Server.Games;

public interface IGameHost
{
    string GameType { get; }
    string GameName { get; }
    Guid Id { get; }
    Task Start();
    bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error);
    Task CancelAsync(string reason);
}