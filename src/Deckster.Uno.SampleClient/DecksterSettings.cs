using System.Security.Cryptography.X509Certificates;

namespace Deckster.CrazyEights.SampleClient;

public class DecksterSettings
{
    public string ServerUrl { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Token { get; set; }
    public string GameName { get; set; }
}