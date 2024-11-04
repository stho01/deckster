using Deckster.Core.Games.Common;
using Deckster.Games;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Controllers;

public class GameVm
{
    public string Name { get; init; }
    public GameState State { get; init; }
    public ICollection<PlayerData> Players { get; init; } = [];
}