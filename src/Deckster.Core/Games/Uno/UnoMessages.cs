using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Uno;

public abstract class UnoGameNotification: DecksterNotification;

public class PlayerPutCardNotification : UnoGameNotification
{
    public Guid PlayerId { get; init; }
    public UnoCard Card { get; init; }
}

public class PlayerPutWildNotification : UnoGameNotification
{
    public Guid PlayerId { get; init; }
    public UnoCard Card { get; init; }
    public UnoColor NewColor { get; init; }
}

public class PlayerDrewCardNotification : UnoGameNotification
{
    public Guid PlayerId { get; init; }
}

public class PlayerPassedNotification : UnoGameNotification
{
    public Guid PlayerId { get; init; }
}

public class ItsYourTurnNotification : UnoGameNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameStartedNotification : UnoGameNotification
{
    public Guid GameId { get; init; }
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameEndedNotification : UnoGameNotification
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
