using Deckster.Core.Games.Common;

namespace Deckster.Games.Gabong;

public class GabongGameCreatedEvent : GameCreatedEvent
{
    public List<PlayerData> Players { get; init; } = [];
    public List<Card> Deck { get; init; } = [];
}