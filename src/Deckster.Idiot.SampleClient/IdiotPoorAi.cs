using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Deckster.Client.Games.Idiot;
using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Idiot;
using Deckster.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace Deckster.Idiot.SampleClient;

public class IdiotPoorAi
{
    private readonly TaskCompletionSource _tcs = new();

    private readonly IdiotState _state = new();
    private readonly IdiotClient _client;
    private readonly ILogger _logger;
    

    public IdiotPoorAi(IdiotClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
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
            Id = p.Id,
            Name = p.Name,
            CardsFacingUp = p.CardsFacingUp,
            CardsFacingDownCount = p.CardsFacingDownCount,
            CardsOnHandCount = p.CardsOnHandCount,
        });

        await _client.IamReadyAsync();
    }
    
    private void PlayerSwappedCards(PlayerSwappedCardsNotification n)
    {
        if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
        {
            _logger.LogInformation("{player} swapped. now on hand: {hand}. now facing up: {facingUp}", player.Name, n.CardNowOnHand, n.CardNowFacingUp);
            player.CardsFacingUp.Remove(n.CardNowOnHand);
            player.CardsFacingUp.Add(n.CardNowFacingUp);
            player.KnownCardsOnHand.Add(n.CardNowOnHand);
        }
    }
    
    private void PlayerIsReady(PlayerIsReadyNotification n)
    {
        if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
        {
            _logger.LogInformation("{player} ready", player.Name);
        }
    }

    private int turn = 0;
    
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private async void ItsMyTurn(ItsYourTurnNotification n)
    {

        await _semaphore.WaitAsync();
        _logger.LogInformation("It's my turn. Top of Pile: '{top}'", _state.TopOfPile);

        try
        {
            while (!_tcs.Task.IsCompleted)
            {
                turn++;

                var playFrom = _state.GetPlayFrom();
                switch (playFrom)
                {
                    case PlayFrom.Hand:
                    {
                        if (CanPutFrom(_state.CardsOnHand, out var cards))
                        {
                            _logger.LogInformation("{turn} Playing {cards} from {playFrom}", turn,
                                string.Join(", ", cards), playFrom);
                            _state.DiscardPile.AddRange(cards);
                            var drawn = await _client.PutCardsFromHandAsync(cards);
                            if (drawn.Any())
                            {
                                _logger.LogInformation("{turn} Drew cards: {cards}", turn, string.Join(", ", cards));
                            }

                            _state.CardsOnHand.AddRange(drawn);

                            if (!_state.DisposeDiscardPile())
                            {
                                return;
                            }

                            _logger.LogInformation("{turn} I disposed discard pile", turn);
                        }
                        else
                        {
                            _logger.LogInformation("{turn} Pulling in ({playFrom})", turn, playFrom);
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
                            _logger.LogInformation("{turn} Playing {cards} from {playFrom}", turn,
                                string.Join(", ", cards), playFrom);
                            await _client.PutCardsFacingUpAsync(cards);
                            _state.DiscardPile.AddRange(cards);
                            if (!_state.DisposeDiscardPile())
                            {
                                return;
                            }

                            _logger.LogInformation("{turn} I disposed discard pile", turn);
                        }
                        else
                        {
                            _logger.LogInformation("{turn} Pulling in ({playFrom})", turn, playFrom);
                            var pulledIn = await _client.PullInDiscardPileAsync();
                            _state.DiscardPile.Clear();
                            _state.CardsOnHand.AddRange(pulledIn);
                            return;
                        }

                        break;
                    }
                    case PlayFrom.FacingDown:
                    {
                        _logger.LogInformation("{turn} Playing card facing down", turn);
                        var r = await _client.PutCardFacingDownAsync(0);
                        _logger.LogInformation("{turn} Played: {card}", turn, r.attemptedCard);
                        _state.CardsFacingDownCount--;

                        if (r.pullInCards.Any())
                        {
                            _logger.LogInformation("{turn} Pulling in:   {count} {cards}", turn, r.pullInCards.Length,
                                string.Join(", ", r.pullInCards));
                            _logger.LogInformation("{turn} Discard pile: {count} {cards}", turn,
                                _state.DiscardPile.Count, string.Join(", ", _state.DiscardPile));
                            _state.CardsOnHand.AddRange(r.pullInCards);
                            _state.DiscardPile.Clear();
                            return;
                        }

                        _state.DiscardPile.Add(r.attemptedCard);
                        if (!_state.DisposeDiscardPile())
                        {
                            return;
                        }

                        _logger.LogInformation("I disposed discard pile");
                        break;
                    }

                    case PlayFrom.Nothing:
                    default:
                        _logger.LogInformation("I can do NOTHING (Am I done?)");
                        return;
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{turn} urk", turn);
            _logger.LogInformation(_state.Pretty());
        }
        finally
        {
            _logger.LogInformation("{turn} Done", turn);
            _semaphore.Release();
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
            .GroupBy(p => p.Rank)
            .ToDictionary(p => p.Key, p => p);
        
        if (groups.Any() && groups.OrderBy(g => g.Key).First().Value.Any())
        {
            cards = groups.First().Value.ToArray();
            list.RemoveAll(cards);
            return true;
        }

        if (list.TryGetFirst(c => c.Rank == 2, out var card))
        {
            cards = [card];
            list.RemoveAll(cards);
            return true;
        }

        if (list.TryGetFirst(c => c.Rank == 10, out card))
        {
            cards = [card];
            list.RemoveAll(cards);
            return true;
        }

        cards = default;
        return false;
    }

    private async void PlayerPulledInDiscardPile(PlayerPulledInDiscardPileNotification n)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
            {
                _logger.LogInformation("{player} pulled in discard pile", player.Name);
                var removed = _state.DiscardPile.ToList();
                player.KnownCardsOnHand.PushRange(removed);
                player.CardsOnHandCount += removed.Count;
                _state.DiscardPile.Clear();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async void PlayerAttemptedPuttingCards(PlayerAttemptedPuttingCardNotification n)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
            {
                _logger.LogInformation("{player} attempted putting '{card}' on '{top}'", player.Name, n.Card, _state.TopOfPile);
                player.KnownCardsOnHand.Add(n.Card);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async void PlayerDrewCards(PlayerDrewCardsNotification n)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
            {
                _logger.LogInformation("{player} drew {number} cards", player.Name, n.NumberOfCards);
                player.CardsOnHandCount += n.NumberOfCards;
                _state.StockPileCount -= n.NumberOfCards;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async void PlayerIsDone(PlayerIsDoneNotification n)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
            {
                _logger.LogInformation("{player} is done", player.Name);
                player.IsDone = true;
            }
            else
            {
                _logger.LogInformation("I AM DONE!");
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async void PlayerPutCards(PlayerPutCardsNotification n)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
            {
                _logger.LogInformation("{player} put {cards} from {where}", player.Name, string.Join(", ", n.Cards), n.From);

                switch (n.From)
                {
                    case PutCardFrom.Hand:
                        player.KnownCardsOnHand.RemoveAll(n.Cards);
                        player.CardsOnHandCount -= n.Cards.Length;
                        break;
                    case PutCardFrom.FacingUp:
                        player.CardsFacingUp.RemoveAll(n.Cards);
                        break;
                    case PutCardFrom.FacingDown:
                        player.CardsFacingDownCount--;
                        break;
                }
            
                _state.DiscardPile.AddRange(n.Cards);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async void DiscardPileFlushed(DiscardPileFlushedNotification n)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_state.OtherPlayers.TryGetValue(n.PlayerId, out var player))
            {
                _logger.LogInformation("{player} flushed discard pile", player.Name);
                _state.FlushedCards.AddRange(_state.DiscardPile);
                _state.DiscardPile.Clear();    
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void GameEnded(GameEndedNotification n)
    {
        _logger.LogInformation("Game ended");
        _tcs.SetResult();
    }

    private void GameHasStarted(GameStartedNotification n)
    {
        _logger.LogInformation("Game started");
    }

    public Task PlayAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.Register(_tcs.SetCanceled);
        return _tcs.Task;
    }
}