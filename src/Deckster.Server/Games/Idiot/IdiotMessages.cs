using Deckster.Client.Games.Common;
using Deckster.Client.Games.Idiot;
using Deckster.Client.Protocol;

namespace Deckster.Server.Games.Idiot;

public class IdiotRequest : DecksterRequest;

public class IamReadyRequest : DecksterRequest;

public class SwapCardsRequest : DecksterRequest
{
    public Card CardOnHand { get; init; }
    public Card CardFacingUp { get; init; }
}

public class PutCardsFromHandRequest : DecksterRequest
{
    public Card[] Cards { get; init; }
}

public class PutCardsFacingUpRequest : DecksterRequest
{
    public Card[] Cards { get; init; }
}

public class PutCardFacingDownRequest : DecksterRequest
{
    public int Index { get; init; }
}

public class DrawCardsRequest : IdiotRequest
{
    public int NumberOfCards { get; init; }
}

public class PullInDiscardPileRequest : IdiotRequest;

public class PutChanceCardRequest : IdiotRequest;

public class IdiotResponse : DecksterResponse;

public class SwapCardsResponse : DecksterResponse
{
    public Card CardNowOnHand { get; init; }
    public Card CardNowFacingUp { get; init; }
}

public class PullInResponse : IdiotResponse
{
    public Card[] Cards { get; init; }
}

public class DrawCardsResponse : IdiotResponse
{
    public Card[] Cards { get; init; }
}

public class PutBlindCardResponse : IdiotResponse
{
    public Card AttemptedCard { get; init; }
    public Card[] PullInCards { get; init; }
}


public class IdiotNotification : DecksterNotification;

public class PlayerSwappedCardsNotification : DecksterNotification
{
    public Guid PlayerId { get; init; }
    public Card CardNowOnHand { get; init; }
    public Card CardNowFacingUp { get; init; }
}

public class PlayerPutCardsNotification : IdiotNotification
{
    public Guid PlayerId { get; init; }
    public Card[] Cards { get; init; }
}

public class PlayerIsReadyNotification : IdiotNotification
{
    public Guid PlayerId { get; init; }
}

public class PlayerIsDoneNotification : IdiotNotification
{
    public Guid PlayerId { get; init; }
}

public class DiscardPileFlushedNotification : IdiotNotification
{
    public Guid PlayerId { get; init; }
}

public class ItsYourTurnNotification : IdiotNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class PlayerDrewCardsNotification : IdiotNotification
{
    public Guid PlayerId { get; init; }
    public int NumberOfCards { get; init; }
}

public class PlayerAttemptedPuttingCardNotification : IdiotNotification
{
    public Guid PlayerId { get; init; }
    public Card Card { get; init; }
}

public class PlayerPulledInDiscardPileNotification : IdiotNotification
{
    public Guid PlayerId { get; init; }
}

public class GameStartedNotification : IdiotNotification;

public class GameEndedNotification : IdiotNotification;

public class ItsTimeToSwapCards : IdiotNotification;
