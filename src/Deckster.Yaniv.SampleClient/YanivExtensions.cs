using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Collections;
using Deckster.Core.Games.Common;

namespace Deckster.Yaniv.SampleClient;

internal static class YanivExtensions
{
    public static Card[] GetCardsToPlay(this IList<Card> cards)
    {
        if (cards.TryFindStraight(out var found))
        {
            return found;
        }

        if (cards.TryFindEquals(out found))
        {
            return found;
        }

        return [cards.MaxBy(c => c.Rank)];
    }

    public static bool TryFindEquals(this IList<Card> cards, [MaybeNullWhen(false)] out Card[] foundCards)
    {
        var group = cards.Where(c => !c.IsJoker())
            .GroupBy(c => c.Rank)
            .OrderByDescending(g => g.Count()).ThenBy(g => g.Key)
            .First();
        
        foundCards = group.ToArray();
        cards.RemoveAll(foundCards);
        
        return true;
    }

    public static bool TryFindStraight(this IList<Card> cards, [MaybeNullWhen(false)] out Card[] foundCards)
    {
        foundCards = default;
        return false;
    }
}