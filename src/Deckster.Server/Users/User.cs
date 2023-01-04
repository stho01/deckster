namespace Deckster.Server.Users;

public class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string AccessToken { get; init; } = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";
    public string Name { get; init; } = "New player";
}