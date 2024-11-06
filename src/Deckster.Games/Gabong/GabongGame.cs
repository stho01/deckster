using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Gabong;
using Deckster.Games.Collections;

namespace Deckster.Games.Gabong;

public class GabongGame : GameObject
{
    public event NotifyPlayer<GameStartedNotification>? GameStarted;
    public event NotifyAll<PlayerPutCardNotification>? PlayerPutCard;
    public event NotifyAll<PlayerPutWildNotification>? PlayerPutWild;
    public event NotifyAll<PlayerDrewCardNotification>? PlayerDrewCard;
    public event NotifyAll<PlayerPassedNotification>? PlayerPassed;
    public event NotifyAll<GameEndedNotification>? GameEnded;
    public event NotifyPlayer<ItsYourTurnNotification>? ItsYourTurn;
    public event NotifyPlayer<RoundStartedNotification>? RoundStarted;
    public event NotifyPlayer<RoundEndedNotification>? RoundEnded; 
    
    private readonly int _initialCardsPerPlayer = 7;

    public int CurrentPlayerIndex { get; set; }
    public int CardsDrawn { get; set; }
    public int GameDirection {get; set;} = 1;

    public override GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;
    
    /// <summary>
    /// All the (shuffled) cards in the game
    /// </summary>
    public List<Card> Deck { get; init; } = [];

    /// <summary>
    /// Where players draw cards from
    /// </summary>
    public List<Card> StockPile { get; } = new();
    
    /// <summary>
    /// Where players put cards
    /// </summary>
    public List<Card> DiscardPile { get; } = new();

    /// <summary>
    /// All the players
    /// </summary>
    public List<GabongPlayer> Players { get; init; } = [];
 
    public Suit? NewColor { get; set; }
    public Card TopOfPile => DiscardPile.Peek();
    public Suit CurrentColor => NewColor ?? TopOfPile.Suit;
    
    public GabongPlayer CurrentPlayer => State == GameState.Finished ? GabongPlayer.Null : Players[CurrentPlayerIndex];

    public static GabongGame Create(GabongGameCreatedEvent created)
    {
        var game = new GabongGame
        {
            Id = created.Id,
            StartedTime = created.StartedTime,
            Players = created.Players.Select(p => new GabongPlayer
            {
                Id = p.Id,
                Name = p.Name
            }).ToList(),
            Deck = created.Deck,
            Seed = created.InitialSeed
        };
        game.NewRound();
        return game;
    }

    public void ScoreRound(GabongPlayer winner)
    {
        winner.Score += Players.Where(x => x.Id != winner.Id).Sum(p => p.CalculateHandScore());
    }

    private void NewRound()
    {
        foreach (var player in Players)
        {
            player.Cards.Clear();
        }
        
        CurrentPlayerIndex = 0;
        StockPile.Clear();
        StockPile.PushRange(Deck);
        for (var ii = 0; ii < _initialCardsPerPlayer; ii++)
        {
            foreach (var player in Players)
            {
                player.Cards.Add(StockPile.Pop());
            }
        }
        
        DiscardPile.Clear();
        DiscardPile.Push(StockPile.Pop());
    }
    
    public async Task<PlayerViewOfGame> PutCard(PutCardRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        var card = request.Card;
        
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

        if (!CanPut(card))
        {
            response = new PlayerViewOfGame($"Cannot put '{card}' on '{TopOfPile}'");
            await RespondAsync(playerId, response);
            return response;
        }
        
        if (CardsDrawn < 0)
        {
            response = new PlayerViewOfGame($"You have to draw {CardsDrawn*-1} cards");
            await RespondAsync(playerId, response);
            return response;
        }
        
        player.Cards.Remove(card);
        DiscardPile.Push(card);
        NewColor = null;
        if (!player.Cards.Any())
        {
            ScoreRound(player);
            NewRound();
            response = GetPlayerViewOfGame(player);
            await RespondAsync(playerId, response);
            return response;
        }

        if(card.Rank == 2)
        {
            CardsDrawn = -2;
        }
        else if(card.Rank == 13)
        {
            GameDirection *= -1;
        }
        else if(card.Rank == 3)
        {
            MoveToNextPlayer();
        }
     
        response = GetPlayerViewOfGame(player);
        await RespondAsync(playerId, response);

        await PlayerPutCard.InvokeOrDefault(() => new PlayerPutCardNotification
        {
            Card = card,
            PlayerId = playerId
        });

        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }

     
    
    public async Task<PlayerViewOfGame> PutWild(PutWildRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        var card = request.Card;
        var newColor = request.NewSuit;
        
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
            response = new PlayerViewOfGame($"{card} is not an 8");
            await RespondAsync(playerId, response);
            return response;
        }

        if (!CanPut(card))
        {
            response = NewColor.HasValue
                ? new PlayerViewOfGame($"Cannot put '{card}' on '{TopOfPile}' (new suit: '{NewColor.Value}')")
                : new PlayerViewOfGame($"Cannot put '{card}' on '{TopOfPile}'");
            await RespondAsync(playerId, response);
            return response;
        }

        player.Cards.Remove(card);
        DiscardPile.Push(card);
        NewColor = newColor;
        if (!player.Cards.Any())
        {
            ScoreRound(player);
            NewRound();
            response = GetPlayerViewOfGame(player);
            await RespondAsync(playerId, response);
            return response;
        }
        
        response = GetPlayerViewOfGame(player);
        await RespondAsync(playerId, response);
        
        await PlayerPutWild.InvokeOrDefault(() => new PlayerPutWildNotification
        {
            PlayerId = playerId,
            Card = card,
            NewSuit = newColor
        });

        await MoveToNextPlayerOrFinishAsync();
        return response;
    }

     
    
    public async Task<GabongCardResponse> DrawCard(DrawCardRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        GabongCardResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new GabongCardResponse{ Error = "It is not your turn" };
            await RespondAsync(playerId, response);
            return response;
        }
  
        if (CardsDrawn == 1)
        {
            response = new GabongCardResponse{ Error = "You can only draw 1 card, then pass if you can't play" };
            await RespondAsync(playerId, response);
            return response;
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            response = new GabongCardResponse{ Error = "No more cards" };
            await RespondAsync(playerId, response);
            return response;
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        CardsDrawn++;
        
        response = new GabongCardResponse
        {
            Card = card
        };
        await RespondAsync(playerId, response);

        await PlayerDrewCard.InvokeOrDefault(() => new PlayerDrewCardNotification
        {
            PlayerId = playerId
        });
        
        if (CardsDrawn == 0) //we just paid the last penalty. Now we skip our turn
        {
            await MoveToNextPlayerOrFinishAsync();
        }
        
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

        if (CardsDrawn != 1)
        {
            response = new EmptyResponse("You have to draw a card first");
            await RespondAsync(playerId, response);
            return response;
        }
        
        response = EmptyResponse.Ok;
        await PlayerPassed.InvokeOrDefault(() => new PlayerPassedNotification
        {
            PlayerId = playerId
        });
        
        await RespondAsync(playerId, response);
        
        await MoveToNextPlayerOrFinishAsync();

        return response;
    }

     
    
    private PlayerViewOfGame GetPlayerViewOfGame(GabongPlayer player)
    {
        return new PlayerViewOfGame
        {
            Cards = player.Cards,
            TopOfPile = TopOfPile,
            CurrentSuit = CurrentColor,
            DiscardPileCount = DiscardPile.Count,
            StockPileCount = StockPile.Count,
            OtherPlayers = Players.Where(p => p.Id != player.Id).Select(ToOtherPlayer).ToList()
        };
    }
    
    private bool TryGetCurrentPlayer(Guid playerId, [MaybeNullWhen(false)] out GabongPlayer player)
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
    
    private async Task MoveToNextPlayerOrFinishAsync()
    {
        if (State == GameState.Finished)
        {
            await GameEnded.InvokeOrDefault(() => new GameEndedNotification());
            return;
        }
        
        MoveToNextPlayer();
        await ItsYourTurn.InvokeOrDefault(CurrentPlayer.Id, () => new ItsYourTurnNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(CurrentPlayer)
        });
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
            index+=GameDirection;
            if (index >= Players.Count)
            {
                index = 0;
            }

            if (index < 0)
            {
                index = Players.Count - 1;
            }
            foundNext = Players[index].IsStillPlaying();
        }

        CurrentPlayerIndex = index;
        CardsDrawn = 0;
    }

    private bool CanPut(Card card)
    {
        return CurrentColor == card.Suit ||
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
        var reshuffledCards = DiscardPile.KnuthShuffle(Seed);
        DiscardPile.Clear();
        DiscardPile.Push(topOfPile);
        StockPile.PushRange(reshuffledCards);
    }

    private static OtherGabongPlayer ToOtherPlayer(GabongPlayer player)
    {
        return new OtherGabongPlayer
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
}