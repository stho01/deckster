namespace Deckster.Server.Collections;

public static class ListAsStackExtensions
{
    public static void PushRange<T>(this List<T> list, IEnumerable<T> items)
    {
        list.AddRange(items);
    }

    public static void Push<T>(this List<T> list, T item)
    {
        list.Add(item);
    }

    public static T Pop<T>(this List<T> list)
    {
        var last = list.Last();
        list.RemoveAt(list.Count - 1);
        return last;
    }

    public static T Peek<T>(this List<T> list)
    {
        return list.Last();
    }
}