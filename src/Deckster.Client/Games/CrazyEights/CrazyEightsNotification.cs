using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.CrazyEights;

public abstract class CrazyEightsNotification : DecksterNotification;

public class PlayerPutCardNotification : CrazyEightsNotification
{
    public Guid PlayerId { get; set; }
    public Card Card { get; set; }
}

public class PlayerPutEightNotification : CrazyEightsNotification
{
    public Guid PlayerId { get; set; }
    public Card Card { get; set; }
    public Suit NewSuit { get; set; }
}

public class PlayerDrewCardNotification : CrazyEightsNotification
{
    public Guid PlayerId { get; set; }
}

public class PlayerPassedNotification : CrazyEightsNotification
{
    public Guid PlayerId { get; set; }
}

public class ItsYourTurnNotification : CrazyEightsNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; } = new();
}

public class GameStartedNotification : CrazyEightsNotification
{
    public Guid GameId { get; init; }
    public PlayerViewOfGame PlayerViewOfGame { get; init; } = new();
}

public class GameEndedNotification : CrazyEightsNotification
{
    public List<PlayerData> Players { get; init; } = [];
}

public class YouAreDoneNotification : CrazyEightsNotification;

public class PlayerIsDoneNotification : CrazyEightsNotification
{
    public Guid PlayerId { get; init; }
}