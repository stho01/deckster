namespace Deckster.Client.Common;

public class PlayerData
{
    public string Name { get; init; } = "Ing. Kognito";
    public Guid Id { get; init; }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}