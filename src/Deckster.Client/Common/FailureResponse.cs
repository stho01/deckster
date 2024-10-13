using Deckster.Client.Protocol;

namespace Deckster.Client.Common;

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

