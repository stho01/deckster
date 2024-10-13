using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Uno;

public abstract class UnoResponse : DecksterResponse;

public class UnoCardsResponse : UnoResponse
{
    public UnoCard Card { get; init; }

    public UnoCardsResponse()
    {
        
    }

    public UnoCardsResponse(UnoCard card)
    {
        Card = card;
    }
}