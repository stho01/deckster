using Deckster.Core.Games.Common;

namespace Deckster.Games.Yaniv;

public class YanivGameCreatedEvent : GameCreatedEvent
{
    public List<Card> Deck { get; init; }
    public List<PlayerData> Players { get; init; } = [];
}