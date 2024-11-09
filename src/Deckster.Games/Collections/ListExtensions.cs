using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Games.Common;

namespace Deckster.Games.Collections;

public static class ListExtensions
{
    public static Card Steal(this List<Card> cards, int rank, Suit suit) => cards.Steal(new Card(rank, suit));

    public static bool Contains(this List<Card> cards, int rank, Suit suit)
    {
        return cards.Contains(new Card(rank, suit));
    }
    
    public static bool HaveSameValue(this IList<Card> cards, out int value)
    {
        value = default;
        if (cards.Count == 0)
        {
            return false;
        }
        value = cards[0].GetValue(ValueCaluclation.AceIsFourteen);
        return cards.All(c => c.Rank == cards[0].Rank);
    }

    public static List<T> StealAll<T>(this List<T> items)
    {
        var stolen = items.ToList();
        items.Clear();
        return stolen;
    }
    
    public static T Steal<T>(this List<T> items, T item)
    {
        if (!items.Remove(item))
        {
            throw new InvalidOperationException($"List does not contain {item}");
        }

        return item;
    }
    
    public static T StealRandom<T>(this List<T> items)
    {
        var item = items.Random();
        if (!items.Remove(item))
        {
            throw new InvalidOperationException($"List does not contain {item}");
        }

        return item;
    }
    
    public static T StealRandom<T>(this List<T> items, int seed)
    {
        var item = items.Random(seed);
        if (!items.Remove(item))
        {
            throw new InvalidOperationException($"List does not contain {item}");
        }

        return item;
    }
    
    public static bool TryStealRange<T>(this List<T> items, IList<T> toSteal, [MaybeNullWhen(false)] out IList<T> stolen)
    {
        stolen = default;
        if (!items.ContainsAll(toSteal))
        {
            return false;
        }

        stolen = toSteal.Select(items.Steal).ToList();

        return true;
    }
    
    public static bool TryStealAt<T>(this List<T> items, int index, [MaybeNullWhen(false)] out T item)
    {
        return items.TryPeekAt(index, out item) && items.Remove(item);
    }

    public static bool TryPeekAt<T>(this List<T> items, int index, [MaybeNullWhen(false)] out T item)
    {
        item = default;
        if (index < 0 || index >= items.Count)
        {
            return false;
        }
        
        item = items[index];
        return true;
    }
}