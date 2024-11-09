using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Yaniv;

public class PutCardsRequest : DecksterRequest
{
    public Card[] Cards { get; init; }
    public DrawCardFrom DrawCardFrom { get; init; }
}

public enum DrawCardFrom
{
    StockPile,
    DiscardPile
}

public class CallYanivRequest : DecksterRequest;

public class PutCardsResponse : DecksterResponse
{
    public Card DrawnCard { get; init; }
}

public class PlayerPutCardsNotification : DecksterNotification
{
    public Guid PlayerId { get; init; }
    public Card[] Cards { get; init; }
    public DrawCardFrom DrewCardFrom { get; init; }
}

public class PlayerViewOfGame
{
    public int DeckSize { get; init; }
    public Card TopOfPile { get; init; }
    public List<Card> CardsOnHand { get; init; }
    public OtherYanivPlayer[] OtherPlayers { get; init; } = [];
}

public class OtherYanivPlayer
{
    public Guid Id { get; init; }
    public int NumberOfCards { get; init; }
    public string Name { get; init; }
}

public class RoundStartedNotification : DecksterNotification
{
    public PlayerViewOfGame PlayerViewOfGame { get; init; }
}

public class ItsYourTurnNotification : DecksterNotification;

public class DiscardPileShuffledNotification : DecksterNotification
{
    public int StockPileSize { get; set; }
    public int DiscardPileSize { get; set; }
}

public class PlayerRoundScore
{
    public Guid PlayerId { get; init; }
    public Card[] Cards { get; init; }
    public int Points { get; set; }
    public int Penalty { get; set; }
    public int TotalPoints => Points + Penalty;
}

public class RoundEndedNotification : DecksterNotification
{
    public Guid WinnerPlayerId { get; init; }
    public PlayerRoundScore[] PlayerScores { get; init; }
}

public class PlayerFinalScore
{
    public Guid PlayerId { get; init; }
    public int Points { get; set; }
    public int Penalty { get; set; }
    public int TotalPoints => Points + Penalty;
    public int FinalPoints { get; set; }
}

public class GameEndedNotification : DecksterNotification
{
    public Guid WinnerPlayerId { get; init; }
    public PlayerFinalScore[] PlayerScores { get; init; }
}