namespace Deckster.Server.Games.Common;

public static class SugarExtensions
{
    
    public static void PushRange<T>(this Stack<T> set, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            set.Push(item);
        }
    }

   
}