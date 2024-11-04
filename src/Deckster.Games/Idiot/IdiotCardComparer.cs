using Deckster.Core.Games.Common;

namespace Deckster.Games.Idiot;

public class IdiotCardComparer : IComparer<Card>
{
    public static readonly IdiotCardComparer Instance = new();
    
    public int Compare(Card x, Card y)
    {
        return x.GetValue(ValueCaluclation.AceIsFourteen)
            .CompareTo(y.GetValue(ValueCaluclation.AceIsFourteen));
    }
}