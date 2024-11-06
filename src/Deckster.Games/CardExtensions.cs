using Deckster.Core.Games.Common;

namespace Deckster.Games;

public enum ValueCaluclation
{
    AceIsFourteen,
    AceIsOne
}

public static class CardExtensions
{
    public static int GetValue(this Card card, ValueCaluclation caluclation)
    {
        return caluclation switch
        {
            ValueCaluclation.AceIsFourteen => card.Rank == 1 ? 14 : card.Rank,
            _ => card.Rank
        };
    }
}