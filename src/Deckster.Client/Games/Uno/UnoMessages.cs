using Deckster.Client.Common;
using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Uno;

public class UnoGameMessage:DecksterMessage
{
    protected override string Discriminator => "uno";
}

public class PlayerPutCardMessage : UnoGameMessage
{
    public OtherUnoPlayer Player { get; set; }
    public UnoCard Card { get; set; }
}

public class PlayerPutWildMessage : UnoGameMessage
{
    public OtherUnoPlayer Player { get; set; }
    public UnoCard Card { get; set; }
    public UnoColor NewColor { get; set; }
}

public class PlayerDrewCardMessage : UnoGameMessage
{
    public OtherUnoPlayer Player { get; set; }
}

public class PlayerPassedMessage : UnoGameMessage
{
    public OtherUnoPlayer Player { get; set; }
}

public class ItsYourTurnMessage : UnoGameMessage
{
    public PlayerViewOfUnoGame PlayerViewOfGame { get; init; }
}

public class GameStartedMessage : UnoGameMessage
{
    public List<PlayerData> Players { get; init; }
}

public class GameEndedMessage : UnoGameMessage
{
    public List<PlayerData> Players { get; init; }
}

public class RoundStartedMessage
{
    public PlayerViewOfUnoGame PlayerViewOfGame { get; init; }
}

public class RoundEndedMessage
{
    public List<PlayerData> Players { get; init; }
}
