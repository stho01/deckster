namespace Deckster.CodeGenerator.CSharp;

public static class HashSetExtensions
{
    public static void AddIfNotNull<T>(this HashSet<T> set, T? item) where T : class
    {
        if (item != null)
        {
            set.Add(item);
        }
    }

    public static void AddRangeIfNotNull<T>(this HashSet<T> set, IEnumerable<T?> items) where T : class
    {
        foreach (var item in items)
        {
            set.AddIfNotNull(item);
        }
    }
}