namespace Deckster.Client.Games.Uno;

public class OtherUnoPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public int NumberOfCards { get; init; }

    public override string ToString()
    {
        return Name;
    }
}