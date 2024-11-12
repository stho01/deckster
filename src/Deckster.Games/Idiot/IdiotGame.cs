using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Idiot;
using Deckster.Games.Collections;

namespace Deckster.Games.Idiot;

public class IdiotGame : GameObject
{
    public event NotifyPlayer<ItsTimeToSwapCardsNotification> ItsTimeToSwapCards; 
    public event NotifyAll<PlayerIsReadyNotification> PlayerIsReady;
    public event NotifyAll<GameStartedNotification> GameHasStarted; 
    public event NotifyAll<GameEndedNotification>? GameEnded;
    public event NotifyPlayer<ItsYourTurnNotification>? ItsYourTurn;
    public event NotifyAll<PlayerDrewCardsNotification>? PlayerDrewCards;
    public event NotifyAll<PlayerPutCardsNotification>? PlayerPutCards;
    public event NotifyAll<DiscardPileFlushedNotification>? DiscardPileFlushed;
    public event NotifyAll<PlayerIsDoneNotification>? PlayerIsDone;
    public event NotifyAll<PlayerSwappedCardsNotification> PlayerSwappedCards;
    
    public event NotifyAll<PlayerAttemptedPuttingCardNotification> PlayerAttemptedPuttingCard;
    public event NotifyAll<PlayerPulledInDiscardPileNotification> PlayerPulledInDiscardPile;
    
    public bool HasStarted { get; set; }
    protected override GameState GetState() => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;
    public int CurrentPlayerIndex { get; set; }
    public IdiotPlayer CurrentPlayer => State == GameState.Finished ? IdiotPlayer.Null : Players[CurrentPlayerIndex];
    /// <summary>
    /// All the (shuffled) cards in the game
    /// </summary>
    public List<Card> Deck { get; init; } = [];
    
    /// <summary>
    /// Where players draw cards from
    /// </summary>
    public List<Card> StockPile { get; init; } = [];
    
    /// <summary>
    /// Where players put cards
    /// </summary>
    public List<Card> DiscardPile { get; init; } = [];
    
    /// <summary>
    /// Pile of garbage, when a user plays a 10 or 4 of same number
    /// </summary>
    public List<Card> GarbagePile { get; init; } = [];

    public Card? TopOfPile => DiscardPile.PeekOrDefault();
    
    public Guid LastCardPutBy { get; set; }
    
    /// <summary>
    /// Done players
    /// </summary>
    public List<IdiotPlayer> DonePlayers { get; init; } = [];
    
    /// <summary>
    /// All the players
    /// </summary>
    public List<IdiotPlayer> Players { get; init; } = [];

    public static IdiotGame Create(IdiotGameCreatedEvent created)
    {
        var game = new IdiotGame
        {
            Id = created.Id,
            StartedTime = created.StartedTime,
            Seed = created.InitialSeed,
            Deck = created.Deck,
            Players = created.Players.Select(p => new IdiotPlayer
            {
                Id = p.Id,
                Name = p.Name
            }).ToList()
        };
        return game;
    }

    public void Deal()
    {
        DiscardPile.Clear();
        StockPile.Clear();
        StockPile.PushRange(Deck);
        
        for (var ii = 0; ii < 3; ii++)
        {
            foreach (var player in Players)
            {
                player.CardsFacingDown.Push(StockPile.StealRandom(IncrementSeed()));
                player.CardsFacingUp.Push(StockPile.StealRandom(IncrementSeed()));
                player.CardsOnHand.Push(StockPile.StealRandom(IncrementSeed()));
            }
        }
    }

    public async Task<EmptyResponse> IamReady(IamReadyRequest request)
    {
        EmptyResponse response;

        var player = Players.FirstOrDefault(p => p.Id == request.PlayerId);
        if (player == null)
        {
            response = new EmptyResponse("You are not playing this game");
            await RespondAsync(request.PlayerId, response);
            return response;
        }
        
        if (HasStarted)
        {
            response = new EmptyResponse("Game has already started");
            await RespondAsync(request.PlayerId, response);
            return response;
        }
        
        player.IsReady = true;

        response = new EmptyResponse();
        await RespondAsync(request.PlayerId, response);
        await PlayerIsReady.InvokeOrDefault(new PlayerIsReadyNotification {PlayerId = player.Id});

        if (Players.All(p => p.IsReady))
        {
            HasStarted = true;
            foreach (var p in Players)
            {
                p.CardsOnHand.Sort(IdiotCardComparer.Instance);
            }

            var startingPlayer = Players
                .OrderBy(p => p.CardsOnHand[0], IdiotCardComparer.Instance)
                .ThenBy(p => p.CardsOnHand[1], IdiotCardComparer.Instance)
                .ThenBy(p => p.CardsOnHand[2], IdiotCardComparer.Instance)
                .First();

            CurrentPlayerIndex = Players.IndexOf(startingPlayer);
            HasStarted = true;
            await GameHasStarted.InvokeOrDefault(() => new GameStartedNotification());
            await ItsYourTurn.InvokeOrDefault(startingPlayer.Id, () => new ItsYourTurnNotification());
        }

        return response;
    }

    public async Task<SwapCardsResponse> SwapCards(SwapCardsRequest request)
    {
        IncrementSeed();

        SwapCardsResponse response;

        if (HasStarted)
        {
            response = new SwapCardsResponse { Error = "Game has started" };
            await RespondAsync(request.PlayerId, response);
            return response;
        }
        
        var player = Players.FirstOrDefault(p => p.Id == request.PlayerId);
        if (player == null)
        {
            // ¯\_(ツ)_/¯
            response = new SwapCardsResponse {Error = "You are not playing this game" };
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        if (!player.CardsOnHand.Remove(request.CardOnHand))
        {
            response = new SwapCardsResponse {Error = "You don't have that card on hand" };
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        if (!player.CardsFacingUp.Remove(request.CardFacingUp))
        {
            response = new SwapCardsResponse {Error = "You don't have that card facing up" };
            await RespondAsync(request.PlayerId, response);
            return response;
        }
        player.CardsOnHand.Push(request.CardFacingUp);
        player.CardsFacingUp.Push(request.CardOnHand);

        response = new SwapCardsResponse
        {
            CardNowFacingUp = request.CardOnHand,
            CardNowOnHand = request.CardFacingUp
        };
        await RespondAsync(request.PlayerId, response);

        await PlayerSwappedCards.InvokeOrDefault(new PlayerSwappedCardsNotification
        {
            PlayerId = player.Id,
            CardNowFacingUp = request.CardOnHand,
            CardNowOnHand = request.CardFacingUp
        });

        return response;
    }

    public async Task<DrawCardsResponse> PutCardsFromHand(PutCardsFromHandRequest request)
    {
        IncrementSeed();
        DrawCardsResponse response;
        if (!TryGetCurrentPlayer(request.PlayerId, out var player, out var error))
        {
            response = new DrawCardsResponse{Error = error};
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        if (!TryPutFromCollection(player, request.Cards, player.CardsOnHand, out var discardPileFlushed, out error))
        {
            response = new DrawCardsResponse{ Error = error};
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        var drawnCards = StockPile.PopUpTo(3 - player.CardsOnHand.Count).ToArray(); 
        player.CardsOnHand.PushRange(drawnCards);
        response = new DrawCardsResponse
        {
            Cards = drawnCards
        };
        
        await RespondAsync(player.Id, response);
        await NotifyPlayerPutCardAsync(player, request.Cards, discardPileFlushed);
        if (response.Cards.Any())
        {
            await PlayerDrewCards.InvokeOrDefault(() => new PlayerDrewCardsNotification
            {
                PlayerId = player.Id,
                NumberOfCards = response.Cards.Length
            });
        }
        
        if (player.IsStillPlaying() && discardPileFlushed)
        {
            return response;
        }
        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }

    public async Task<EmptyResponse> PutCardsFacingUp(PutCardsFacingUpRequest request)
    {
        IncrementSeed();
        EmptyResponse response;
        if (!TryGetCurrentPlayer(request.PlayerId, out var player, out var error))
        {
            response = new EmptyResponse(error);
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        if (player.CardsOnHand.Any())
        {
            response = new EmptyResponse("You still have cards on hand");
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        if (StockPile.Any())
        {
            response = new EmptyResponse("There are still cards in stock pile");
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        if (!TryPutFromCollection(player, request.Cards, player.CardsFacingUp, out var discardPileFlushed, out error))
        {
            response = new EmptyResponse(error);
            await RespondAsync(request.PlayerId, response);
            return response;
        }
        
        response = EmptyResponse.Ok;
        await RespondAsync(player.Id, response);
        await NotifyPlayerPutCardAsync(player, request.Cards, discardPileFlushed);
        if (player.IsStillPlaying() && discardPileFlushed)
        {
            return response;
                
        }

        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }
    
    public async Task<PutBlindCardResponse> PutCardFacingDown(PutCardFacingDownRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        var index = request.Index;
        
        PutBlindCardResponse response;
        
        if (!TryGetCurrentPlayer(playerId, out var player, out var error))
        {
            response = new PutBlindCardResponse{ Error = error };
            await RespondAsync(playerId, response);
            return response;
        }

        if (player.CardsOnHand.Any())
        {
            response = new PutBlindCardResponse{ Error = "You still have cards on hand" };
            await RespondAsync(playerId, response);
            return response;
        }
        
        if (player.CardsFacingUp.Any())
        {
            response = new PutBlindCardResponse{ Error = "You still have cards facing up" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (StockPile.Any())
        {
            response = new PutBlindCardResponse{ Error = "There are still cards in stock pile" };
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        if (!player.CardsFacingDown.TryStealAt(index, out var card))
        {
            response = new PutBlindCardResponse{ Error = "Invalid face down card index" };
            await RespondAsync(player.Id, response);
            return response;
        }

        return await PlayBlindCardAsync(player, card);
    }

    public async Task<PutBlindCardResponse> PutChanceCard(PutChanceCardRequest request)
    {
        var playerId = request.PlayerId;

        PutBlindCardResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player, out var error))
        {
            response = new PutBlindCardResponse{ Error = error };
            await RespondAsync(playerId, response);
            return response;
        }

        if (CanPlay(player))
        {
            response = new PutBlindCardResponse{ Error = "You must play one of your cards" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (!StockPile.TryPop(out var card))
        {
            response = new PutBlindCardResponse{ Error = "Stock pile is empty" };
            await RespondAsync(playerId, response);
            return response;
        }

        return await PlayBlindCardAsync(player, card);
    }
    
    private async Task<PutBlindCardResponse> PlayBlindCardAsync(IdiotPlayer player, Card card)
    {
        PutBlindCardResponse response;
        if (TryPut(player.Id, [card], out var discardPileFlushed, out _))
        {
            response = new PutBlindCardResponse
            {
                AttemptedCard = card,
                PullInCards = []
            };

            await RespondAsync(player.Id, response);

            await NotifyPlayerPutCardAsync(player, [card], discardPileFlushed);
            if (player.IsStillPlaying() && discardPileFlushed)
            {
                return response;
            }
            
            await MoveToNextPlayerOrFinishAsync();

            return response;
        }

        var cards = DiscardPile.StealAll().Append(card).ToArray();
        player.CardsOnHand.AddRange(cards);
            
        response = new PutBlindCardResponse
        {
            AttemptedCard = card,
            PullInCards = cards.ToArray()
        };
        await RespondAsync(player.Id, response);
            
        await PlayerAttemptedPuttingCard.InvokeOrDefault(() => new PlayerAttemptedPuttingCardNotification{ PlayerId = player.Id, Card = card });
        await PlayerPulledInDiscardPile.InvokeOrDefault(() => new PlayerPulledInDiscardPileNotification{ PlayerId = player.Id });
            
        await MoveToNextPlayerOrFinishAsync();
            
        return response;
    }

    public async Task<PullInResponse> PullInDiscardPile(PullInDiscardPileRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;

        PullInResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player, out var error))
        {
            response = new PullInResponse{ Error = error };
            await RespondAsync(playerId, response);
            return response;
        }

        if (CanPlay(player))
        {
            response = new PullInResponse{ Error = "You must play one of your cards" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (DiscardPile.Count == 0)
        {
            response = new PullInResponse{ Error = "Discard pile is empty" };
            await RespondAsync(playerId, response);
            return response;
        }

        var cards = DiscardPile.StealAll();
        player.CardsOnHand.AddRange(cards);

        response = new PullInResponse
        {
            Cards = cards.ToArray()
        };
        await RespondAsync(playerId, response);

        await PlayerPulledInDiscardPile.InvokeOrDefault(() => new PlayerPulledInDiscardPileNotification{ PlayerId = playerId });

        await MoveToNextPlayerOrFinishAsync();

        return response;
    }

    private bool CanPlay(IdiotPlayer player)
    {
        if (player.CardsOnHand.Count > 0)
        {
            return player.CardsOnHand.Any(CanPut);
        }

        if (player.CardsFacingUp.Count > 0)
        {
            return player.CardsFacingUp.Any(CanPut);
        }

        return player.CardsFacingDown.Count > 0;
    }
      

    private bool TryPutFromCollection(IdiotPlayer player, Card[] cards, List<Card> collection, out bool discardPileFlushed, [MaybeNullWhen(true)] out string error)
    {
        discardPileFlushed = false;
        if (!collection.ContainsAll(cards))
        {
            error = "You don't have those cards";
            return false;
        }

        if (TryPut(player.Id, cards, out discardPileFlushed, out error))
        {
            collection.RemoveAll(cards);
            return true;
        }

        return false;
    }
    
    private async Task NotifyPlayerPutCardAsync(IdiotPlayer player, Card[] cards, bool discardPileFlushed)
    {
        await PlayerPutCards.InvokeOrDefault(() => new PlayerPutCardsNotification{ PlayerId = player.Id, Cards = cards });
        
        // If player is still playing, player must:
        // - draw card
        // - If discard pile was flushed: put a new card, then draw card
        if (player.IsStillPlaying() && (discardPileFlushed || StockPile.Any()))
        {
            return;
        }

        if (discardPileFlushed)
        {
            await DiscardPileFlushed.InvokeOrDefault(() => new DiscardPileFlushedNotification
            {
                PlayerId = player.Id
            });
        }

        if (!player.IsStillPlaying())
        {
            DonePlayers.Add(player);
            await PlayerIsDone.InvokeOrDefault(() => new PlayerIsDoneNotification { PlayerId = player.Id });
        }
    }
    
    

    private bool TryPut(Guid playerId, Card[] cards, out bool discardPileFlushed, [MaybeNullWhen(true)] out string error)
    {
        discardPileFlushed = false;

        if (!CanPut(cards, out var rank, out error))
        {
            return false;
        }
        
        DiscardPile.PushRange(cards);
        LastCardPutBy = playerId;

        var last = DiscardPile.TakeLast(4).ToArray();
        if (rank == 10 || last.Length == 4 && last.AreOfSameRank())
        {
            GarbagePile.PushRange(DiscardPile);
            DiscardPile.Clear();
            discardPileFlushed = true;
        }

        return true;
    }

    private bool CanPut(Card card) => CanPut([card], out _, out _);
    
    private bool CanPut(Card[] cards, out int value, [MaybeNullWhen(true)] out string error)
    {
        value = default;
        if (cards.Length < 1)
        {
            error = "You must put at least 1 card";
            return false;
        }
        
        if (!cards.HaveSameValue(out value))
        {
            error = "All cards must have same rank";
            return false;
        }
        
        var currentValue = TopOfPile?.GetValue(ValueCaluclation.AceIsFourteen);
        if (currentValue.HasValue && value < currentValue && value != 2 && value != 10)
        {
            error = $"Rank ({value}) must be equal to or higher than current rank ({currentValue})";
            return false;
        }

        error = default;
        return true;
    }
    
    private async Task MoveToNextPlayerOrFinishAsync()
    {
        if (State == GameState.Finished)
        {
            await GameEnded.InvokeOrDefault(() => new GameEndedNotification());
            return;
        }
        
        MoveToNextPlayer();
        await ItsYourTurn.InvokeOrDefault(CurrentPlayer.Id, () =>  new ItsYourTurnNotification());
    }
    
    private PlayerViewOfGame GetPlayerViewOfGame(IdiotPlayer player)
    {
        return new PlayerViewOfGame
        {
            CardsOnHand = player.CardsOnHand,
            CardsFacingUp = player.CardsFacingUp,
            CardsFacingDownCount = player.CardsFacingDown.Count,
            StockPileCount = StockPile.Count,
            OtherPlayers = Players.Where(p => p.Id != player.Id).Select(ToOtherPlayer).ToList()
        };
    }
    
    private static OtherIdiotPlayer ToOtherPlayer(IdiotPlayer player)
    {
        return new OtherIdiotPlayer
        {
            Id = player.Id,
            Name = player.Name,
            CardsOnHandCount = player.CardsOnHand.Count,
            CardsFacingUp = player.CardsFacingUp,
            CardsFacingDownCount = player.CardsFacingDown.Count
        };
    }
    
    private void MoveToNextPlayer()
    {
        if (Players.Count(p => p.IsStillPlaying()) < 2)
        {
            return;
        }

        var foundNext = false;
        
        var index = CurrentPlayerIndex;
        while (!foundNext)
        {
            index++;
            if (index >= Players.Count)
            {
                index = 0;
            }

            foundNext = Players[index].IsStillPlaying();
        }

        CurrentPlayerIndex = index;
    }

    private bool TryGetCurrentPlayer(Guid playerId, [MaybeNullWhen(false)] out IdiotPlayer player, [MaybeNullWhen(true)] out string error)
    {
        player = default;
        error = default;
        if (!HasStarted)
        {
            error = "Game has not yet started";
            return false;
        }
        
        var p = CurrentPlayer;
        if (p.Id != playerId)
        {
            player = default;
            error = "It is not your turn";
            return false;
        }

        player = p;
        return true;
    }
    
    public override Task StartAsync()
    {
        if (HasStarted)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(Players.Select(p =>
            ItsTimeToSwapCards.InvokeOrDefault(p.Id, () => new ItsTimeToSwapCardsNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(p)
        })));
    }
}