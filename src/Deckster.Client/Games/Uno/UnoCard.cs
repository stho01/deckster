namespace Deckster.Client.Games.Uno;

public class UnoCard
{
    public UnoColor Color { get; }
    public UnoValue Value { get; }

    public UnoCard(UnoColor color, UnoValue value)
    {
        Color = color;
        Value = value;
    }

    public override string ToString()
    {
        if(Value is UnoValue.Wild or UnoValue.WildDrawFour)
        {
            return $"{Value}";
        }
        return $"{Color} {Value}";
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