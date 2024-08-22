using System.Diagnostics.CodeAnalysis;
using Deckster.Server.Communication;
using Deckster.Server.Games.CrazyEights;

namespace Deckster.Server.Games;

public interface IGameHost
{
    event EventHandler<CrazyEightsGameHost> OnEnded;
    Guid Id { get; }
    Task Start();
    bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error);
    Task CancelAsync(string reason);
}