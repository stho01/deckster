using Deckster.Client.Games.Uno;
using Deckster.Core.Games.Uno;
using Deckster.Games.Collections;

namespace Deckster.UnitTests.Games.Uno;

public static class UnoListExtensions
{
    public static UnoCard Get(this List<UnoCard> cards, UnoValue value, UnoColor suit) => cards.Steal(new UnoCard(value, suit));
}