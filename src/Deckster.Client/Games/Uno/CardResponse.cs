using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Uno;

public class UnoCardResponse : DecksterResponse
{
    public UnoCard Card { get; init; }
}