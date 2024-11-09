using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Uno;

public class PlayerPutCardNotification : DecksterNotification
{
    public Guid PlayerId { get; init; }
    public UnoCard Card { get; init; }
}

public class PlayerPutWildNotification : DecksterNotification
{
    public Guid PlayerId { get; init; }
    public UnoCard Card { get; init; }
    public UnoColor NewColor { get; init; }
}

public class PlayerDrewCardNotification : DecksterNotification
{
    public Guid PlayerId { get; init; }
}

public class PlayerPassedNotification : DecksterNotification
{
    public Guid PlayerId { get; init; }
}

public class ItsYourTurnNotification : DecksterNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameStartedNotification : DecksterNotification
{
    public Guid GameId { get; init; }
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameEndedNotification : DecksterNotification
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
