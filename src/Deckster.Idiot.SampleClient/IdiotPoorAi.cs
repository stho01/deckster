using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Idiot;
using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Idiot;

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

public enum PlayFrom
{
    Hand,
    FacingUp,
    FacingDown,
    Nothing
}

public class OtherPlayer
{
    public List<Card> KnownCardsOnHand { get; init; } = [];
    public int CardsOnHandCount { get; set; }
    public List<Card> CardsFacingUp { get; init; }
    public int CardsFacingDownCount { get; set; }
    public bool IsDone { get; set; }
}

public class IdiotPoorAi
{
    private readonly TaskCompletionSource _tcs = new();

    private readonly IdiotState _state = new();
    private readonly IdiotClient _client;
    

    public IdiotPoorAi(IdiotClient client)
    {
        _client = client;
        client.PlayerSwappedCards += PlayerSwappedCards;
        client.ItsTimeToSwapCards += ItsTimeToSwapCards;
        client.PlayerIsReady += PlayerIsReady;

        client.GameHasStarted += GameHasStarted;
        client.GameEnded += GameEnded;
        client.ItsYourTurn += ItsMyTurn;
        client.PlayerDrewCards += PlayerDrewCards;
        client.PlayerPutCards += PlayerPutCards;
        client.DiscardPileFlushed += DiscardPileFlushed;
        client.PlayerIsDone += PlayerIsDone;
        
        client.PlayerAttemptedPuttingCard += PlayerAttemptedPuttingCards;
        client.PlayerPulledInDiscardPile += PlayerPulledInDiscardPile;
    }
    
    private async void ItsTimeToSwapCards(ItsTimeToSwapCardsNotification n)
    {
        var view = n.PlayerViewOfGame;
        _state.CardsOnHand.PushRange(view.CardsOnHand);
        _state.CardsFacingUp.PushRange(view.CardsFacingUp);
        _state.StockPileCount = view.StockPileCount;
        _state.CardsFacingDownCount = view.CardsFacingDownCount;

        _state.OtherPlayers = view.OtherPlayers.ToDictionary(p => p.Id, p => new OtherPlayer
        {
            CardsFacingUp = p.CardsFacingUp,
            CardsFacingDownCount = p.CardsFacingDownCount,
            CardsOnHandCount = p.CardsOnHandCount,
        });

        await _client.IamReadyAsync();
    }
    
    private void PlayerSwappedCards(PlayerSwappedCardsNotification n)
    {
        
    }
    
    private void PlayerIsReady(PlayerIsReadyNotification n)
    {
        
    }
    
    private async void ItsMyTurn(ItsYourTurnNotification n)
    {
        var turn = 0;
        
        while (!_tcs.Task.IsCompleted)
        {
            turn++;
            switch (_state.GetPlayFrom())
            {
                case PlayFrom.Hand:
                {
                    if (CanPutFrom(_state.CardsOnHand, out var cards))
                    {
                        _state.CardsOnHand.RemoveAll(cards);
                        _state.DiscardPile.AddRange(cards);
                        var drawn = await _client.PutCardsFromHandAsync(cards);
                        _state.CardsOnHand.AddRange(drawn);

                        if (!_state.DisposeDiscardPile())
                        {
                            return;
                        }
                    }
                    else
                    {
                        var pulledIn = await _client.PullInDiscardPileAsync();
                        _state.DiscardPile.Clear();
                        _state.CardsOnHand.AddRange(pulledIn);
                    
                        return;
                    }

                    break;
                }
                case PlayFrom.FacingUp:
                {
                    if (CanPutFrom(_state.CardsFacingUp, out var cards))
                    {
                        _state.CardsFacingUp.RemoveAll(cards);
                        await _client.PutCardsFacingUpAsync(cards);
                        _state.DiscardPile.AddRange(cards);
                        if (!_state.DisposeDiscardPile())
                        {
                            return;
                        }
                    }
                    else
                    {
                        var pulledIn = await _client.PullInDiscardPileAsync();
                        _state.DiscardPile.Clear();
                        _state.CardsOnHand.AddRange(pulledIn);
                        return;
                    }

                    break;
                }
                case PlayFrom.FacingDown:
                {
                    var r = await _client.PutCardFacingDownAsync(0);
                    _state.CardsFacingDownCount--;

                    if (r.pullInCards.Any())
                    {
                        _state.CardsOnHand.AddRange(r.pullInCards);
                        _state.DiscardPile.Clear();
                        return;
                    }

                    _state.DiscardPile.Add(r.attemptedCard);
                    if (!_state.DisposeDiscardPile())
                    {
                        return;
                    }
                    break;
                }
                
                case PlayFrom.Nothing:
                default:
                    return;
            }
        }
    }

    private bool CanPutFrom(List<Card> list, [MaybeNullWhen(false)] out Card[] cards)
    {
        if (list.Count == 0)
        {
            cards = default;
            return false;
        }
        var groups = list
            .Where(c => c.Rank != 2 && c.Rank != 10 && c.GetValue(ValueCaluclation.AceIsFourteen) >= _state.TopOfPile?.GetValue(ValueCaluclation.AceIsFourteen))
            .GroupBy(c => c.GetValue(ValueCaluclation.AceIsFourteen))
            .OrderBy(c => c.Key);
        
        if (groups.Any() && groups.First().Any())
        {
            cards = groups.First().ToArray();
            return true;
        }

        if (list.TryGetFirst(c => c.Rank == 2, out var card))
        {
            cards = [card];
            return true;
        }

        if (list.TryGetFirst(c => c.Rank == 10, out card))
        {
            cards = [card];
            return true;
        }

        cards = default;
        return false;
    }

    private void PlayerPulledInDiscardPile(PlayerPulledInDiscardPileNotification n)
    {
        if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
        {
            var removed = _state.DiscardPile.ToList();
            player.KnownCardsOnHand.PushRange(removed);
            player.CardsOnHandCount += removed.Count;
        }
        _state.DiscardPile.Clear();
    }

    private void PlayerAttemptedPuttingCards(PlayerAttemptedPuttingCardNotification n)
    {
        if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
        {
            player.KnownCardsOnHand.Add(n.Card);
        }
    }

    private void PlayerDrewCards(PlayerDrewCardsNotification n)
    {
        
    }

    private void PlayerIsDone(PlayerIsDoneNotification n)
    {
        if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
        {
            player.IsDone = true;
        }
    }

    private void PlayerPutCards(PlayerPutCardsNotification n)
    {
        if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
        {
            player.KnownCardsOnHand.RemoveAll(n.Cards);
            _state.DiscardPile.PushRange(n.Cards);
        }
    }

    

    private void DiscardPileFlushed(DiscardPileFlushedNotification n)
    {
        _state.FlushedCards.AddRange(_state.DiscardPile);
        _state.DiscardPile.Clear();
    }

    private void GameEnded(GameEndedNotification n)
    {
        _tcs.SetResult();
    }

    private void GameHasStarted(GameStartedNotification n)
    {
        
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(_tcs.SetCanceled);
        return _tcs.Task;
    }
}