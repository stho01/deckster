using Deckster.Client.Games.Common;
using Deckster.Client.Games.Uno;

namespace Deckster.Server.Games.Uno.Core;

public class UnoGameCreatedEvent : GameCreatedEvent
{
    public List<PlayerData> Players { get; init; } = [];
    public List<UnoCard> Deck { get; init; } = [];
}