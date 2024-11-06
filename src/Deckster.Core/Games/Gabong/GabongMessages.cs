using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Gabong;

public abstract class GabongGameNotification: DecksterNotification;

public class PlayerPutCardNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
    public Card Card { get; init; }
}

public class PlayerPutWildNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
    public Card Card { get; init; }
    public Suit NewSuit { get; init; }
}

public class PlayerDrewCardNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
}

public class PlayerPassedNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
}

public class ItsYourTurnNotification : GabongGameNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameStartedNotification : GabongGameNotification
{
    public Guid GameId { get; init; }
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameEndedNotification : GabongGameNotification
{
    public List<PlayerData> Players { get; init; }
}

public class RoundStartedNotification : DecksterNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class RoundEndedNotification : DecksterNotification
{
    public List<PlayerData> Players { get; init; }
}
