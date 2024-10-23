using Deckster.Server.Data;

namespace Deckster.Server.Games;

public class GameObject : DatabaseObject
{
    public DateTimeOffset StartedTime { get; init; }
    public int Version { get; set; }
}
