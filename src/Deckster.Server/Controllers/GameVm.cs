using Deckster.Client.Games.Common;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Controllers;

public class GameVm
{
    public string Id { get; init; }
    public GameState State { get; init; }
    public ICollection<PlayerData> Players { get; init; } = [];
}