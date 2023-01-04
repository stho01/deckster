namespace Deckster.Client.Common;

public class FailureResult : CommandResult
{
    public string Message { get; }
    
    public FailureResult(string message)
    {
        Message = message;
    }
}