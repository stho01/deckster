using Deckster.Client.Games.Uno;

namespace Deckster.Server.Games.Uno.Core;

public class Deck
{
    public List<UnoCard> Cards { get; }

    public Deck(IEnumerable<UnoCard> cards)
    {
        Cards = cards.ToList();
    }

    public Deck Shuffle()
    {
        Cards.KnuthShuffle();
        return this;
    }
    
    public static Deck Standard
    {
        get
        {
            var ret = new List<UnoCard>();
            foreach (var color in Enum.GetValues<UnoColor>())
            {
                if (color == UnoColor.Wild)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        ret.Add(new UnoCard(color, UnoValue.Wild));
                        ret.Add(new UnoCard(color, UnoValue.WildDrawFour));
                    }
                }
                else
                {
                    for (var i = 0; i < 2; i++)
                    {
                        foreach (var value in Enum.GetValues<UnoValue>())
                        {
                            if (value == UnoValue.Wild || value == UnoValue.WildDrawFour)
                            {
                                continue;
                            }
                            if(value == UnoValue.Zero && i == 1)
                            {
                                continue;
                            }
                            ret.Add(new UnoCard(color, value));
                        }
                    }
                }
            }
            
            return new Deck(ret);
        }
    }
}


public static class UnoDeckExtensions
{
    
  
    
    public static List<UnoCard> KnuthShuffle(this List<UnoCard> cards, DateTimeOffset? shufflePerformedAtEpochTime = null)
    {
        var random = new Random((int)(shufflePerformedAtEpochTime??DateTimeOffset.UtcNow).Ticks%int.MaxValue);
        var ii = cards.Count;
        while (ii > 1)
        {
            var k = random.Next(ii--);
            (cards[ii], cards[k]) = (cards[k], cards[ii]);
        }

        return cards;
    }
    
    public static IEnumerable<UnoCard> Shuffle(this IEnumerable<UnoCard> cards, DateTimeOffset? shufflePerformedAtEpochTime = null)
    {
        var random = new Random((int)(shufflePerformedAtEpochTime??DateTimeOffset.UtcNow).Ticks%int.MaxValue);
        return cards.OrderBy(c => random.Next());
    }
    
    
}