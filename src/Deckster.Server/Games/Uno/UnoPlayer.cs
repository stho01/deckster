using Deckster.Client.Games.Uno;

namespace Deckster.Server.Games.Uno;

public class UnoPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    
    public List<UnoCard> Cards { get; } = new();

    public static readonly UnoPlayer Null = new()
    {
        Id = Guid.Empty,
        Name = "Ing. Kognito"
    };

    public bool HasCard(UnoCard card) => Cards.Contains(card);

    public bool IsStillPlaying() => Cards.Any();

    public int Score { get; set; }

    public bool HasWon => Score >= 500;

    public int CalculateHandScore()
    {
        return Cards.Sum(card => card.Value switch
        {
            UnoValue.Wild => 50,
            UnoValue.WildDrawFour => 50,
            UnoValue.DrawTwo => 20,
            UnoValue.Skip => 20,
            UnoValue.Reverse => 20,
            UnoValue.Zero => 0,
            _ => (int)card.Value
        });
    }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}