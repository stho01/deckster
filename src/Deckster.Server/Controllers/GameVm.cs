using Deckster.Client.Common;

namespace Deckster.Server.Controllers;

public class GameVm
{
    public Guid Id { get; init; }
    public ICollection<PlayerData> Players { get; init; } = [];
}