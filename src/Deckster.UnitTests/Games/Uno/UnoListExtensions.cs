using Deckster.Client.Games.Uno;

namespace Deckster.UnitTests.Games.Uno;

public static class UnoListExtensions
{
    public static UnoCard Get(this List<UnoCard> cards, UnoValue value, UnoColor suit) => cards.Steal(new UnoCard(value, suit));
}