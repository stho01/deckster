using Deckster.Core.Games.Common;
using Deckster.Games.Collections;

namespace Deckster.UnitTests.Games.Gabong;

public static class GabongListExtensions
{
    public static Card Get(this List<Card> cards, int rank, Suit suit) => cards.Steal(new Card(rank, suit));
}