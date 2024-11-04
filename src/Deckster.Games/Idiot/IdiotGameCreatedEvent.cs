using Deckster.Core.Games.Common;

namespace Deckster.Games.Idiot;

public class IdiotGameCreatedEvent : GameCreatedEvent
{
    public List<PlayerData> Players { get; init; } = [];
    public List<Card> Deck { get; init; }
}