namespace Deckster.Client.Sugar;

public static class StringExtensions
{
    public static int? TryParseToInt(this string input)
    {
        if (int.TryParse(input, out var result))
        {
            return result;
        }

        return null;
    }
}