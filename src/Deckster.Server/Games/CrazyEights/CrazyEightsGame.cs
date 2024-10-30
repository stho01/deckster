using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Server.Collections;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGame : GameObject
{
    public event NotifyPlayer<GameStartedNotification>? GameStarted;
    public event NotifyAll<PlayerDrewCardNotification>? PlayerDrewCard;
    public event NotifyPlayer<ItsYourTurnNotification>? ItsYourTurn;
    public event NotifyAll<PlayerPassedNotification>? PlayerPassed;
    public event NotifyAll<PlayerPutCardNotification>? PlayerPutCard;
    public event NotifyAll<GameEndedNotification>? GameEnded;
    public event NotifyAll<PlayerIsDoneNotification>? PlayerIsDone;
    
    public int InitialCardsPerPlayer { get; set; } = 5;
    public int CurrentPlayerIndex { get; set; }
    public int CardsDrawn { get; set; }

    public int Seed { get; set; }
    
    /// <summary>
    /// Done players
    /// </summary>
    public List<CrazyEightsPlayer> DonePlayers { get; init; } = [];
    
    public override GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;

    /// <summary>
    /// All the (shuffled) cards in the game
    /// </summary>
    public List<Card> Deck { get; init; } = [];

    /// <summary>
    /// Where players draw cards from
    /// </summary>
    public List<Card> StockPile { get; init; } = new();
    
    /// <summary>
    /// Where players put cards
    /// </summary>
    public List<Card> DiscardPile { get; init; } = new();

    /// <summary>
    /// All the players
    /// </summary>
    public List<CrazyEightsPlayer> Players { get; init; } = [];

    public Suit? NewSuit { get; set; }
    public Card TopOfPile => DiscardPile.Peek();
    public Suit CurrentSuit => NewSuit ?? TopOfPile.Suit;

    public CrazyEightsPlayer CurrentPlayer => State == GameState.Finished ? CrazyEightsPlayer.Null : Players[CurrentPlayerIndex];

    public static CrazyEightsGame Create(CrazyEightsGameCreatedEvent created)
    {
        var game = new CrazyEightsGame
        {
            Id = created.Id,
            StartedTime = created.StartedTime,
            Seed = created.InitialSeed,
            Deck = created.Deck,
            Players = created.Players.Select(p => new CrazyEightsPlayer
            {
                Id = p.Id,
                Name = p.Name
            }).ToList()
        };
        game.Reset();

        return game;
    }
    
    private void Reset()
    {
        foreach (var player in Players)
        {
            player.Cards.Clear();
        }
        
        CurrentPlayerIndex = 0;
        DonePlayers.Clear();
        StockPile.Clear();
        StockPile.PushRange(Deck);
        for (var ii = 0; ii < InitialCardsPerPlayer; ii++)
        {
            foreach (var player in Players)
            {
                player.Cards.Add(StockPile.Pop());
            }
        }
        
        DiscardPile.Clear();
        DiscardPile.Push(StockPile.Pop());
        DonePlayers.Clear();
    }

    public async Task<PlayerViewOfGame> PutCard(PutCardRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        var card = request.Card;
        
        PlayerViewOfGame response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new PlayerViewOfGame { Error = "It is not your turn" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (!player.HasCard(card))
        {
            response = new PlayerViewOfGame { Error = $"You don't have '{card}'" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (!CanPut(card))
        {
            response = new PlayerViewOfGame{ Error = $"Cannot put '{card}' on '{TopOfPile}'" };
            await RespondAsync(playerId, response);
            return response;
        }
        
        player.Cards.Remove(card);
        DiscardPile.Push(card);
        NewSuit = null;
        if (!player.Cards.Any())
        {
            DonePlayers.Add(player);
        }

        response = GetPlayerViewOfGame(player);
        await RespondAsync(playerId, response);

        await PlayerPutCard.InvokeOrDefault(new PlayerPutCardNotification {PlayerId = playerId, Card = card});

        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }

    public async Task<PlayerViewOfGame> PutEight(PutEightRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        var card = request.Card;
        var newSuit = request.NewSuit;
        
        PlayerViewOfGame response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new PlayerViewOfGame("It is not your turn");
            await RespondAsync(playerId, response);
            return response;
        }

        if (!player.HasCard(card))
        {
            response = new PlayerViewOfGame($"You don't have '{card}'");
            await RespondAsync(playerId, response);
            return response;
        }
        
        if (card.Rank != 8)
        {
            response = new PlayerViewOfGame("Card rank must be '8'");
            await RespondAsync(playerId, response);
            return response;
        }

        if (!CanPut(card))
        {
            response = NewSuit.HasValue
                ? new PlayerViewOfGame($"Cannot put '{card}' on '{TopOfPile}' (new suit: '{NewSuit.Value}')")
                : new PlayerViewOfGame($"Cannot put '{card}' on '{TopOfPile}'");
            await RespondAsync(playerId, response);
            return response;
        }

        player.Cards.Remove(card);
        DiscardPile.Push(card);
        NewSuit = newSuit != card.Suit ? newSuit : null;
        
        response = GetPlayerViewOfGame(player);
        await RespondAsync(playerId, response);
        
        if (!player.Cards.Any())
        {
            DonePlayers.Add(player);
            
            await PlayerIsDone.InvokeOrDefault(new PlayerIsDoneNotification
            {
                PlayerId = playerId
            });
        }

        await MoveToNextPlayerOrFinishAsync();
        return response;
    }

    

    public async Task<CardResponse> DrawCard(DrawCardRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        CardResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new CardResponse{ Error = "It is not your turn" }; 
            await RespondAsync(playerId, response);
            return response;
        }
        
        if (CardsDrawn > 2)
        {
            response = new CardResponse{ Error = "You can only draw 3 cards" };
            await RespondAsync(playerId, response);
            return response;
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            response = new CardResponse{ Error = "Stock pile is empty" };
            await RespondAsync(playerId, response);
            return response;
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        CardsDrawn++;
        
        response = new CardResponse(card);
        await RespondAsync(playerId, response);

        await PlayerDrewCard.InvokeOrDefault(new PlayerDrewCardNotification
        {
            PlayerId = playerId
        });
        return response;
    }

     

    public async Task<EmptyResponse> Pass(PassRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        EmptyResponse response;
        if (!TryGetCurrentPlayer(playerId, out _))
        {
            response = new EmptyResponse("It is not your turn");
            await RespondAsync(playerId, response);
            return response;
        }
        
        response = EmptyResponse.Ok;
        await RespondAsync(playerId, response);

        await PlayerPassed.InvokeOrDefault(new PlayerPassedNotification
        {
            PlayerId = playerId
        });
        
        await MoveToNextPlayerOrFinishAsync();
        return response;
    }
    
    private async Task MoveToNextPlayerOrFinishAsync()
    {
        if (State == GameState.Finished)
        {
            await GameEnded.InvokeOrDefault(new GameEndedNotification());
            return;
        }
        
        MoveToNextPlayer();
        await ItsYourTurn.InvokeOrDefault(CurrentPlayer.Id, new ItsYourTurnNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(CurrentPlayer)
        });
    }

    

    private PlayerViewOfGame GetPlayerViewOfGame(CrazyEightsPlayer player)
    {
        return new PlayerViewOfGame
        {
            Cards = player.Cards,
            TopOfPile = TopOfPile,
            CurrentSuit = CurrentSuit,
            DiscardPileCount = DiscardPile.Count,
            StockPileCount = StockPile.Count,
            OtherPlayers = Players.Where(p => p.Id != player.Id).Select(ToOtherPlayer).ToList()
        };
    }

    private bool TryGetCurrentPlayer(Guid playerId, [MaybeNullWhen(false)] out CrazyEightsPlayer player)
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
        CardsDrawn = 0;
    }

    private bool CanPut(Card card)
    {
        return CurrentSuit == card.Suit ||
               TopOfPile.Rank == card.Rank ||
               card.Rank == 8;
    }
    
    private void ShufflePileIfNecessary()
    {
        if (StockPile.Any())
        {
            return;
        }
        
        if (DiscardPile.Count < 2)
        {
            return;
        }

        var topOfPile = DiscardPile.Pop();
        var reshuffledCards = DiscardPile.ToList().KnuthShuffle(Seed);
        DiscardPile.Clear();
        DiscardPile.Push(topOfPile);
        StockPile.PushRange(reshuffledCards);
    }

    private static OtherCrazyEightsPlayer ToOtherPlayer(CrazyEightsPlayer player)
    {
        return new OtherCrazyEightsPlayer
        {
            Name = player.Name,
            NumberOfCards = player.Cards.Count
        };
    }

    public override async Task StartAsync()
    {
        foreach (var player in Players)
        {
            await GameStarted.InvokeOrDefault(player.Id, () => new GameStartedNotification
            {
                GameId = Id,
                PlayerViewOfGame = GetPlayerViewOfGame(player)
            });
        }

        await ItsYourTurn.InvokeOrDefault(CurrentPlayer.Id, () => new ItsYourTurnNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(CurrentPlayer)
        });
    }

    
    private void IncrementSeed()
    {
        unchecked
        {
            Seed++;
        }
    }
}