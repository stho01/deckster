namespace Deckster.Server.Controllers;

public class ResponseMessage
{
    public string Message { get; init; }

    public ResponseMessage()
    {
        
    }

    public ResponseMessage(string message)
    {
        Message = message;
    }
}