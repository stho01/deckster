namespace Deckster.Core.Games.Common;

public class PlayerData
{
    public string Name { get; init; } = "Ing. Kognito";
    public double Points { get; set; } = 0;
    public Guid Id { get; init; }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}