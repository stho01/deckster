using Deckster.Client.Common;
using Deckster.Client.Games.Common;

namespace Deckster.Server.Games.CrazyEights.Core;

public class CrazyEightsGameCreatedEvent : GameCreatedEvent
{
    public Guid Id { get; init; }
    public int InitialSeed { get; init; } = DateTimeOffset.UtcNow.Nanosecond;
    public List<PlayerData> Players { get; init; } = [];
    public List<Card> Deck { get; init; } = [];
}
