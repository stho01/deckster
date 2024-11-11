using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Gabong;

public abstract class GabongGameNotification: DecksterNotification;

public class PlayerPutCardNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
    public Card Card { get; init; }
    
    public Suit? NewSuit { get; init; }
}

public class PlayerDrewCardNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
}
public class PlayerDrewPenaltyCardNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
}

public class GameStartedNotification : GabongGameNotification
{
    public Guid GameId { get; init; }
}

public class GameEndedNotification : GabongGameNotification
{
    public List<PlayerData> Players { get; init; }
}

public class RoundStartedNotification : GabongGameNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
    public Guid StartingPlayerId { get; set; }
}

public class RoundEndedNotification : GabongGameNotification
{
    public List<PlayerData> Players { get; init; }
}
public class PlayerLostTheirTurnNotification : GabongGameNotification
{
    public Guid PlayerId { get; init; }
    public PlayerLostTurnReason LostTurnReason { get; set; }
}

public enum PlayerLostTurnReason
{
    Passed,
    WrongPlay,
    TookTooLong,
    FinishedDrawingCardDebt
}

public enum GabongPlay
{
    CardPlayed,
    TurnLost,
    RoundStarted
}
