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

    public static string ToCamelCase(this string input)
    {
        if (char.IsLower(input[0]))
        {
            return input;
        }

        var chars = input.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }
}