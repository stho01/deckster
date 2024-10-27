using Deckster.Server.Data;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games;

public abstract class GameObject : DatabaseObject
{
    protected ICommunication Communication = NullCommunication.Instance;
    
    public DateTimeOffset StartedTime { get; init; }
    public abstract GameState State { get; }

    // ReSharper disable once UnusedMember.Global
    // Used by Marten
    public int Version { get; set; }

    public void SetCommunication(ICommunication communication)
    {
        Communication = communication;
    }

    public abstract Task StartAsync();
}
