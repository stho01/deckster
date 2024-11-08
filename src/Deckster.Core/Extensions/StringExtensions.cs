namespace Deckster.Core.Extensions;

public static class StringExtensions
{
    public static string StringJoined<T>(this IEnumerable<T> items, string separator)
    {
        return string.Join(separator, items);
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
    
    public static string ToPascalCase(this string input)
    {
        if (char.IsUpper(input[0]))
        {
            return input;
        }

        var chars = input.ToCharArray();
        chars[0] = char.ToUpperInvariant(chars[0]);
        return new string(chars);
    }
    
    public static bool Exists(this string? input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }
}