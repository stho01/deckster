using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Uno;

public class UnoCardResponse : DecksterResponse
{
    public UnoCard Card { get; init; }
}