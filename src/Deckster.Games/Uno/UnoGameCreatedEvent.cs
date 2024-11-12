using Deckster.Core.Games.Common;
using Deckster.Core.Games.Uno;

namespace Deckster.Games.Uno;

public class UnoGameCreatedEvent : GameCreatedEvent
{
    public List<PlayerData> Players { get; init; } = [];
    public List<UnoCard> Deck { get; init; } = [];
}