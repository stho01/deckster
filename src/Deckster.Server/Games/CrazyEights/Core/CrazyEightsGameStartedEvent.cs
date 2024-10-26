using Deckster.Client.Games.Common;

namespace Deckster.Server.Games.CrazyEights.Core;

public class CrazyEightsGameCreatedEvent : GameCreatedEvent
{
    public List<PlayerData> Players { get; init; } = [];
    public List<Card> Deck { get; init; } = [];
}
