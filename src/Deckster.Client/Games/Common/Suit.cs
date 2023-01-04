namespace Deckster.Client.Games.Common;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

public static class SuitExtensions
{
    public static string Display(this Suit suit)
    {
        return suit switch
        {
            Suit.Clubs => "♧",
            Suit.Diamonds => "♢",
            Suit.Hearts => "♥",
            Suit.Spades => "♤",
            _ => throw new ArgumentOutOfRangeException(nameof(suit), suit, null)
        };
    }
}