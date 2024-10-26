using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Common;

public class FailureResponse : DecksterResponse
{
    public string Message { get; init; }

    public FailureResponse()
    {
        
    }
    
    public FailureResponse(string message)
    {
        Message = message;
    }
}