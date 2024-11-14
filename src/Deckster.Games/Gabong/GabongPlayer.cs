using Deckster.Core.Games.Common;

namespace Deckster.Games.Gabong;

public class GabongPlayer
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    
    public List<Card> Cards { get; } = new();

    public static readonly GabongPlayer Null = new()
    {
        Id = Guid.Empty,
        Name = "Ing. Kognito"
    };

    public bool HasCard(Card card) => Cards.Contains(card);

    public bool IsStillPlaying() => Cards.Any();

    public int Score { get; set; }

    public bool HasWon => Score >= 500;

    public int CalculateHandScore()
    {
        return Cards.Sum(card => card.Rank switch
        {
            1 => 3,
            13 => 2,
            12 => 2,
            11 => 2,
            _ => 1
        });
    }

    public override string ToString()
    {
        return $"{Name} ({Id})";
    }

    public void ScoreRound()
    {
        Score += CalculateHandScore();
        if (Cards.Count == 0)
        {
            RoundsWon++;
        }
        Cards.Clear();
    }


    public PlayerData ToPlayerData()
    {
        return new PlayerData
        {
            Id = Id,
            Name = Name,
            Points = Score,
            CardsInHand = Cards.Count,
            Info = new()
            {
                ["Penalties"]=Penalties.ToString(),
                ["Gabongs"]=Gabongs.ToString(),
                ["Bongas"]=Bongas.ToString(),
                ["Shots"]=Shots.ToString(),
                ["CardsPlayed"]=CardsPlayed.ToString(),
                ["DebtDrawn"]=DebtDrawn.ToString(),
                ["Passes"]=Passes.ToString(),
                ["RoundsWon"]=RoundsWon.ToString(),
            }
        };
    }

    public int Penalties { get; set; }
    public int Gabongs { get; set; }
    public int Bongas { get; set; }
    public int Shots { get; set; }
    public int CardsPlayed { get; set; }
    public int DebtDrawn { get; set; }
    public int Passes { get; set; }
    public int TurnsLost { get; set; }
    public int RoundsWon { get; set; }
}

