using Deckster.Core.Protocol;

namespace Deckster.Core.Games.Common;

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