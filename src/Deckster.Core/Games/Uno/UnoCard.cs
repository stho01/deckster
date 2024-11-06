namespace Deckster.Core.Games.Uno;

public class UnoCard
{
    public UnoColor Color { get; }
    public UnoValue Value { get; }

    public UnoCard(UnoValue value, UnoColor color)
    {
        Color = color;
        Value = value;
    }

    public override bool Equals(object? obj)
    {
        return obj is UnoCard c && Equals(c);
    }
    
    public static bool operator == (UnoCard first, UnoCard second)
    {
        return first.Equals(second);
    }

    public static bool operator !=(UnoCard first, UnoCard second)
    {
        return !(first == second);
    }

    private bool Equals(UnoCard other)
    {
        return Value == other.Value && Color == other.Color;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, (int) Color);
    }
    
    public override string ToString()
    {
        if(Value is UnoValue.Wild or UnoValue.WildDrawFour)
        {
            return $"{Value}";
        }
        return $"{Value} {Color}";
    }
}

public enum UnoColor
{
    Red,
    Yellow,
    Green,
    Blue,
    Wild
}

public enum UnoValue
{
    Zero = 0,
    One= 1,
    Two= 2,
    Three= 3,
    Four= 4,
    Five= 5,
    Six= 6,
    Seven= 7,
    Eight= 8,
    Nine= 9,
    Skip= 21,
    Reverse= 22,
    DrawTwo= 23,
    Wild= 51,
    WildDrawFour= 52
}