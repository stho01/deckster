using Deckster.Server.Data;

namespace Deckster.Server.Games;

public class GameObject : DatabaseObject
{
    public DateTimeOffset StartedTime { get; init; }
    // ReSharper disable once UnusedMember.Global
    // Used by Marten
    public int Version { get; set; }
}
