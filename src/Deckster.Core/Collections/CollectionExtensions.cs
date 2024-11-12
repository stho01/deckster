using System.Diagnostics.CodeAnalysis;

namespace Deckster.Core.Collections;

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

    public static bool TryGetFirst<T>(this ICollection<T> collection, Func<T, bool> predicate, [MaybeNullWhen(false)] out T value)
    {
        foreach (var item in collection)
        {
            if (predicate(item))
            {
                value = item;
                return true;
            }
        }

        value = default;
        return false;
    }
}