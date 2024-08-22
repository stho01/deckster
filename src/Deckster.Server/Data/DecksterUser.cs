namespace Deckster.Server.Data;

public class DecksterUser : DomainObject
{
    public string AccessToken { get; set; } = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    public string Name { get; set; } = "New player";
    public string Password { get; set; }
}