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
    
    public static List<T> PopUpTo<T>(this List<T> list, int number)
    {
        var popped = new List<T>();
        for (var ii = 0; ii < number; ii++)
        {
            if (!list.TryPop(out var p))
            {
                return popped;
            }
            popped.Add(p);
        }

        return popped;
    }
    
    public static T? PopOrDefault<T>(this List<T> list)
    {
        if (list.IsNullOrEmpty())
        {
            return default;
        }
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


        popped = list.Pop();
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

    public static bool TryPeek<T>(this List<T> list, [MaybeNullWhen(false)] out T item)
    {
        item = default;
        if (list.Count == 0)
        {
            return false;
        }

        item = list.Last();
        return true;
    }
}