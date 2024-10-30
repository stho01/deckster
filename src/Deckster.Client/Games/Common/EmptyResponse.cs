using Deckster.Client.Protocol;

namespace Deckster.Client.Games.Common;

public class EmptyResponse : DecksterResponse
{
    public EmptyResponse()
    {
        
    }
    
    public EmptyResponse(string error)
    {
        Error = error;
    }

    public static readonly EmptyResponse Ok = new();
}