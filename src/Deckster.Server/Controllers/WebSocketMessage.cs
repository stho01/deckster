namespace Deckster.Server.Controllers;

public class WebSocketMessage
{
    public string Type { get; init; }
    public object Data { get; init; }
}