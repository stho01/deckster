using System.Security.Cryptography;

namespace Deckster.Server.Games;

public static class RandomExtensions
{
    public static List<T> KnuthShuffle<T>(this List<T> cards, int seed)
    {
        var random = new Random(seed);
        var ii = cards.Count;
        while (ii > 1)
        {
            var k = random.Next(ii--);
            (cards[ii], cards[k]) = (cards[k], cards[ii]);
        }

        return cards;
    }

    private static int Next(this RandomNumberGenerator random)
    {
        var bytes = new byte[4];
        random.GetBytes(bytes);
        return Convert.ToInt32(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
    }

    public static T Random<T>(this IList<T> items)
    {
        var random = new Random();
        return items[random.Next(items.Count - 1)];
    }
}