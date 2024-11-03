using Deckster.Client.Games.Common;

namespace Deckster.Server.Games.Idiot;

public class IdiotPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public List<Card> CardsOnHand { get; init; } = [];
    public List<Card> CardsFacingUp { get; init; } = [];
    public List<Card> CardsFacingDown { get; init; } = [];
    public bool IsReady { get; set; }

    public bool IsStillPlaying() => CardsOnHand.Any() || CardsFacingUp.Any() || CardsFacingDown.Any();
    public bool IsDone() => !IsStillPlaying();
    
    public static readonly IdiotPlayer Null = new()
    {
        Id = Guid.Empty,
        Name = "Ing. Kognito"
    };

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}