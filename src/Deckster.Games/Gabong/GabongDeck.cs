using Deckster.Core.Games.Common;

namespace Deckster.Games.Gabong;

public static class GabongDeck
{
    public static List<Card> Standard
    {
        get
        {
            var ret = new List<Card>();
            for (int d = 0; d < 3; d++)
            {
                foreach (var color in Enum.GetValues<Suit>())
                {
                    for (var i = 1; i <= 13; i++)
                    {
                        ret.Add(new Card(i, color));
                    }
                }
            }

            return ret;
        }
    }
}