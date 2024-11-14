using Deckster.Core.Games.Common;
using Deckster.Games;

namespace Deckster.Server.Controllers;

public class GameVm
{
    public string GameType { get; init; }
    public string Name { get; init; }
    public GameState State { get; init; }
    public ICollection<PlayerData> Players { get; init; } = [];
}

public class GameOverviewVm
{
    public string GameType { get; init; }
    public List<GameVm> Games { get; init; }
}
