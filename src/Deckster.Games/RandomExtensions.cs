namespace Deckster.Games;

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

    public static T Random<T>(this IList<T> items)
    {
        var random = new Random();
        return items[random.Next(items.Count - 1)];
    }
    
    public static T Random<T>(this IList<T> items, int seed)
    {
        var random = new Random(seed);
        return items[random.Next(items.Count - 1)];
    }
}