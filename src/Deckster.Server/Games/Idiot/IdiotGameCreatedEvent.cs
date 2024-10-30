using Deckster.Client.Games.Common;

namespace Deckster.Server.Games.Idiot;

public class IdiotGameCreatedEvent : GameCreatedEvent
{
    public List<PlayerData> Players { get; init; } = [];
    public List<Card> Deck { get; init; }
}