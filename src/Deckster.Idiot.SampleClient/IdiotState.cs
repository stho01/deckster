using Deckster.Core.Collections;
using Deckster.Core.Games.Common;

namespace Deckster.Idiot.SampleClient;

public class IdiotState
{
    public List<Card> CardsOnHand { get; } = [];
    public List<Card> CardsFacingUp { get; } = [];
    public List<Card> DiscardPile { get; } = [];
    public List<Card> FlushedCards { get; } = [];
    public int StockPileCount { get; set; }

    public Card? TopOfPile => DiscardPile.PeekOrDefault();

    public Dictionary<Guid, OtherPlayer> OtherPlayers { get; set; } = new();
    public int CardsFacingDownCount { get; set; }

    public bool IsStillPlaying()
    {
        return CardsOnHand.Any() || CardsFacingUp.Any() || CardsFacingDownCount > 0;
    }

    public bool DisposeDiscardPile()
    {
        if (!DiscardPile.Any())
        {
            return false;
        }

        if (DiscardPile.Last().Rank == 10)
        {
            FlushedCards.AddRange(DiscardPile);
            DiscardPile.Clear();
            return true;
        }

        var last = DiscardPile.TakeLast(4).ToArray();
        if (last.Length == 4 && last.AreOfSameRank())
        {
            FlushedCards.AddRange(DiscardPile);
            DiscardPile.Clear();
            return true;
        }
        return false;
    }

    public PlayFrom GetPlayFrom()
    {
        if (CardsOnHand.Any())
        {
            return PlayFrom.Hand;
        }

        if (CardsFacingUp.Any())
        {
            return PlayFrom.FacingUp;
        }

        if (CardsFacingDownCount > 0)
        {
            return PlayFrom.FacingDown;
        }

        return PlayFrom.Nothing;
    }
}