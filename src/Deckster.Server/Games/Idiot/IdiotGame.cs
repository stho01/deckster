using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.Idiot;
using Deckster.Server.Collections;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.Idiot;

public class IdiotGame : GameObject
{
    public event NotifyAll<GameEndedNotification>? GameEnded;
    public event NotifyPlayer<ItsYourTurnNotification>? ItsYourTurn;
    public event NotifyAll<PlayerDrewCardsNotification>? PlayerDrewCards;
    public event NotifyAll<PlayerPutCardsNotification>? PlayerPutCards;
    public event NotifyAll<DiscardPileFlushedNotification>? DiscardPileFlushed;
    
    public int Seed { get; set; }
    public override GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;
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
        return new IdiotGame
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
    }

    public async Task<EmptyResponse> PutCardsFromHand(PutCardsFromHandRequest request)
    {
        IncrementSeed();
        EmptyResponse response;
        var playerId = request.PlayerId;
        var cards = request.Cards;
        
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new EmptyResponse("It is not your turn");
            await RespondAsync(playerId, response);
            return response;
        }

        if (cards.Length < 1)
        {
            response = new EmptyResponse("You must put at least 1 card");
            await RespondAsync(playerId, response);
            return response;
        }
        
        if (!player.CardsOnHand.RemoveAll(cards))
        {
            response = new EmptyResponse("You don't have all of those cards");
            await RespondAsync(playerId, response);
            return response;
        }
        
        if (!CardsHaveSameRank(cards, out var rank))
        {
            response = new EmptyResponse("All cards must have same rank");
            await RespondAsync(playerId, response);
            return response;
        }

        var currentRank = TopOfPile?.Rank;
        if (rank < currentRank && rank != 2 && rank != 10)
        {
            response = new EmptyResponse($"Rank ({rank}) must be equal to or higher than current rank ({currentRank})");
            await RespondAsync(playerId, response);
            return response;
        }
        
        DiscardPile.PushRange(cards);
        LastCardPutBy = playerId;

        var discardpileFlushed = false;
        if (rank == 10 || cards.Length == 4)
        {
            GarbagePile.PushRange(DiscardPile);
            DiscardPile.Clear();
            discardpileFlushed = true;
        }
        
        response = EmptyResponse.Ok;
        await RespondAsync(playerId, response);
        
        await PlayerPutCards.InvokeOrDefault(new PlayerPutCardsNotification
        {
            PlayerId = playerId,
            Cards = cards
        });
        
        if (discardpileFlushed)
        {
            await DiscardPileFlushed.InvokeOrDefault(() => new DiscardPileFlushedNotification { PlayerId = playerId }); 
        }
        
        // If player is still playing, player must:
        // - draw card
        // - If discard pile was flushed: put a new card, then draw card
        if (player.IsStillPlaying() && (discardpileFlushed || StockPile.Any()))
        {
            return response;
        }
        
        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }

     
     

    public async Task<EmptyResponse> PutFaceUpTableCard(PutFaceUpTableCardsRequest request)
    {
        IncrementSeed();
        
        EmptyResponse response;
        if (!TryGetCurrentPlayer(request.PlayerId, out var player))
        {
            response = new EmptyResponse("It is not your turn");
            await RespondAsync(request.PlayerId, response);
            return response;
        }

        throw new NotImplementedException();
    }
    
    public async Task<EmptyResponse> PutFaceDownTableCard(PutFaceDownTableCardRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<DrawCardsResponse> DrawCards(DrawCardsRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        var numberOfCards = request.NumberOfCards;
        
        DrawCardsResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new DrawCardsResponse{ Error = "It is not your turn" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (numberOfCards <= 0)
        {
            response = new DrawCardsResponse{ Error = "You have to draw at least 1 card" };
            await RespondAsync(playerId, response);
            return response;
        }

        var max = 3 - player.CardsOnHand.Count;
        if (numberOfCards > 3 - player.CardsOnHand.Count)
        {
            response = new DrawCardsResponse{ Error = $"You can only have {max} more cards on hand" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (!StockPile.TryPop(numberOfCards, out var cards))
        {
            response = new DrawCardsResponse{ Error = "Not enough cards in stock pile" };
            await RespondAsync(playerId, response);
            return response;
        }
        
        player.CardsOnHand.PushRange(cards);
        response = new DrawCardsResponse { Cards = cards };
        await RespondAsync(playerId, response);

        await PlayerDrewCards.InvokeOrDefault(() => new PlayerDrewCardsNotification
        {
            PlayerId = playerId,
            NumberOfCards = numberOfCards
        });

        // Player just flushed discard pile
        // so it is still this player's turn
        if (DiscardPile.IsEmpty() && LastCardPutBy == playerId)
        {
            return response;
        }

        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }

     
    
    private async Task MoveToNextPlayerOrFinishAsync()
    {
        if (State == GameState.Finished)
        {
            await GameEnded.InvokeOrDefault(() => new GameEndedNotification());
            return;
        }
        
        MoveToNextPlayer();
        await ItsYourTurn.InvokeOrDefault(CurrentPlayer.Id, () =>  new ItsYourTurnNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(CurrentPlayer)
        });
    }
    
    private PlayerViewOfGame GetPlayerViewOfGame(IdiotPlayer player)
    {
        return new PlayerViewOfGame
        {
            CardsOnHand = player.CardsOnHand,
            TopOfPile = TopOfPile,
            DiscardPileCount = DiscardPile.Count,
            StockPileCount = StockPile.Count,
            OtherPlayers = Players.Where(p => p.Id != player.Id).Select(ToOtherPlayer).ToList()
        };
    }
    
    private static OtherIdiotPlayer ToOtherPlayer(IdiotPlayer player)
    {
        return new OtherIdiotPlayer
        {
            PlayerId = player.Id,
            Name = player.Name,
            CardsOnHandCount = player.CardsOnHand.Count,
            VisibleTableCards = player.FaceUpTableCards,
            HiddenTableCardsCount = player.FaceDownTableCards.Count
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

    private static bool CardsHaveSameRank(Card[] cards, out int rank)
    {
        rank = default;
        if (cards.Length == 0)
        {
            return false;
        }
        rank = cards[0].Rank;
        return cards.All(c => c.Rank == cards[0].Rank);
    }

    private bool TryGetCurrentPlayer(Guid playerId, [MaybeNullWhen(false)] out IdiotPlayer player)
    {
        var p = CurrentPlayer;
        if (p.Id != playerId)
        {
            player = default;
            return false;
        }

        player = p;
        return true;
    }
    
    private void IncrementSeed()
    {
        unchecked
        {
            Seed++;
        }
    }
    
    public override Task StartAsync()
    {
        throw new NotImplementedException();
    }
}