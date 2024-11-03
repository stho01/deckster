namespace Deckster.Client.Games.Common;

public readonly struct Card
{
    public int Rank { get; init; }
    public Suit Suit { get; init; }

    public Card()
    {
        
    }

    public Card(int rank, Suit suit)
    {
        if (rank is < 0 or > 14)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), $"Invalid rank '{rank}'");
        }
        Rank = rank;
        Suit = suit;
    }

    public static bool operator == (Card first, Card second)
    {
        return first.Equals(second);
    }

    public static bool operator !=(Card first, Card second)
    {
        return !(first == second);
    }

    private bool Equals(Card other)
    {
        return Rank == other.Rank && Suit == other.Suit;
    }

    public override bool Equals(object? obj)
    {
        return obj is Card c && Equals(c);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Rank, (int) Suit);
    }

    public override string ToString()
    {
        return Rank switch
        {
            0 => Map(Rank),
            _ => $"{Map(Rank)}{Suit.Display()}"
        };
    }

    private static string Map(int rank)
    {
        return rank switch
        {
            0 => "Joker",
            1 => "A",
            >= 2 and < 11 => $"{rank}",
            11 => "J",
            12 => "Q",
            13 => "K",
            _ => throw new ArgumentOutOfRangeException(nameof(rank), "Invalid rank '{rank}'")
        };
    }
}