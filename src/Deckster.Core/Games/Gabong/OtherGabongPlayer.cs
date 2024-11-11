namespace Deckster.Core.Games.Gabong;



public class SlimGabongPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public int NumberOfCards { get; init; }

    public override string ToString()
    {
        return Name;
    }
}