namespace Deckster.Games.Collections;

public static class CollectionExtensions
{
    public static bool ContainsAll<T>(this ICollection<T> collection, ICollection<T> items)
    {
        return items.All(collection.Contains);
    }

    public static bool RemoveAll<T>(this ICollection<T> collection, ICollection<T> items)
    {
        if (!collection.ContainsAll(items))
        {
            return false;
        }

        foreach (var item in items)
        {
            collection.Remove(item);
        }

        return true;
    }

    public static bool IsEmpty<T>(this ICollection<T> collection)
    {
        return collection.Count == 0;
    }
    
    public static bool IsNullOrEmpty<T>(this ICollection<T>? collection)
    {
        return collection == null || collection.Count == 0;
    }
}