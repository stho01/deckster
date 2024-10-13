namespace Deckster.Client.Games.Uno;

public class UnoFailureResponse : UnoResponse
{
    public string Message { get; init; }

    public UnoFailureResponse()
    {
        
    }
    
    public UnoFailureResponse(string message)
    {
        Message = message;
    }
}

public class UnoSuccessResponse : UnoResponse
{
    
}

public class UnoCardResponse : UnoResponse
{
    public UnoCard Card { get; init; }

    public UnoCardResponse()
    {
        
    }

    public UnoCardResponse(UnoCard card)
    {
        Card = card;
    }
}