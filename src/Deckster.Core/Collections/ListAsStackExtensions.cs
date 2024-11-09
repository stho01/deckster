using System.Diagnostics.CodeAnalysis;

namespace Deckster.Core.Collections;

public static class ListAsStackExtensions
{
    public static List<T> PushRange<T>(this List<T> list, IEnumerable<T> items)
    {
        list.AddRange(items);
        return list;
    }

    public static List<T> Push<T>(this List<T> list, T item)
    {
        list.Add(item);
        return list;
    }

    public static T Pop<T>(this List<T> list)
    {
        var last = list.Last();
        list.RemoveAt(list.Count - 1);
        return last;
    }

    public static bool TryPop<T>(this List<T> list, [MaybeNullWhen(false)] out T popped)
    {
        if (list.Count < 1)
        {
            popped = default;
            return false;
        }

        popped = list.Last();
        return true;
    }
    
    public static bool TryPop<T>(this List<T> list, int count, [MaybeNullWhen(false)] out T[] popped)
    {
        popped = default;
        if (list.Count < count)
        {
            return false;
        }

        var vgLista = new T[count];
        for (var ii = 0; ii < count; ii++)
        {
            vgLista[ii] = list.Pop(); // Hihi
        }

        popped = vgLista.ToArray();
        return true;
    }

    public static T Peek<T>(this List<T> list)
    {
        return list.Last();
    }
    
    public static T? PeekOrDefault<T>(this List<T> list)
    {
        return list.LastOrDefault();
    }
}