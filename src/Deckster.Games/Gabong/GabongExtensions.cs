using Deckster.Core.Games.Common;

namespace Deckster.Games.Gabong;

public static class GabongExtensions
{
    public static bool IsASpecialCard(this Card card)
    {
        return card.Rank == 2      //draw 2
               || card.Rank == 3   //skip
               || card.Rank == 13; //reverse
    }
}