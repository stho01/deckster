using Deckster.Client.Games.Common;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    
    public List<Card> Cards { get; } = new();

    public static readonly CrazyEightsPlayer Null = new()
    {
         Id = Guid.Empty,
         Name = "Ing. Kognito"
    };

    public bool HasCard(Card card) => Cards.Contains(card);

    public bool IsStillPlaying() => Cards.Any();
}