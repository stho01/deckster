namespace Deckster.Client.Common;

public class PlayerData
{
    public string Name { get; init; }
    public Guid PlayerId { get; init; }

    public int Points { get; set; }
}