using Deckster.Client.Common;
using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.CrazyEights;

public class PlayerPutCardMessage : DecksterMessage
{
    public Guid PlayerId { get; set; }
    public Card Card { get; set; }
}

public class PlayerPutEightMessage : DecksterMessage
{
    public Guid PlayerId { get; set; }
    public Card Card { get; set; }
    public Suit NewSuit { get; set; }
}

public class PlayerDrewCardMessage : DecksterMessage
{
    public Guid PlayerId { get; set; }
}

public class PlayerPassedMessage : DecksterMessage
{
    public Guid PlayerId { get; set; }
}

public class ItsYourTurnMessage : DecksterMessage
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameStartedMessage : DecksterMessage
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class GameEndedMessage : DecksterMessage
{
    public List<PlayerData> Players { get; init; }
}
