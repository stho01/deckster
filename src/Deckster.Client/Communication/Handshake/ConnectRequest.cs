namespace Deckster.Client.Communication.Handshake;

public class ConnectRequest
{
    public string AccessToken { get; init; }
    public int ClientPort { get; init; }
    public string Path { get; init; }
}