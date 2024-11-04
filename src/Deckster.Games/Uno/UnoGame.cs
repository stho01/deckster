using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Uno;
using Deckster.Games.Collections;

namespace Deckster.Games.Uno;

public class UnoGame : GameObject
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
    public List<UnoCard> Deck { get; init; } = [];

    /// <summary>
    /// Where players draw cards from
    /// </summary>
    public List<UnoCard> StockPile { get; } = new();
    
    /// <summary>
    /// Where players put cards
    /// </summary>
    public List<UnoCard> DiscardPile { get; } = new();

    /// <summary>
    /// All the players
    /// </summary>
    public List<UnoPlayer> Players { get; init; } = [];
 
    public UnoColor? NewColor { get; set; }
    public UnoCard TopOfPile => DiscardPile.Peek();
    public UnoColor CurrentColor => NewColor ?? TopOfPile.Color;
    
    public UnoPlayer CurrentPlayer => State == GameState.Finished ? UnoPlayer.Null : Players[CurrentPlayerIndex];

    public static UnoGame Create(UnoGameCreatedEvent created)
    {
        var game = new UnoGame
        {
            Id = created.Id,
            StartedTime = created.StartedTime,
            Players = created.Players.Select(p => new UnoPlayer
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

    public void ScoreRound(UnoPlayer winner)
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

        if(card.Value == UnoValue.DrawTwo)
        {
            CardsDrawn = -2;
        }
        else if(card.Value == UnoValue.Reverse)
        {
            GameDirection *= -1;
        }
        else if(card.Value == UnoValue.Skip)
        {
            MoveToNextPlayer();
        }
        else if(card.Value == UnoValue.WildDrawFour)
        {
            CardsDrawn = -4;
        }

        response = GetPlayerViewOfGame(player);
        await RespondAsync(playerId, response);

        await EventExtensions.InvokeOrDefault(PlayerPutCard, () => new PlayerPutCardNotification
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
        var newColor = request.NewColor;
        
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
        
        if (card.Color != UnoColor.Wild)
        {
            response = new PlayerViewOfGame($"{card} is not a wildcard");
            await RespondAsync(playerId, response);
            return response;
        }

        if (newColor == UnoColor.Wild)
        {
            response = new PlayerViewOfGame("New color cannot be 'Wild'");
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
        
        await EventExtensions.InvokeOrDefault(PlayerPutWild, new PlayerPutWildNotification
        {
            PlayerId = playerId,
            Card = card,
            NewColor = newColor
        });

        await MoveToNextPlayerOrFinishAsync();
        return response;
    }

     
    
    public async Task<UnoCardResponse> DrawCard(DrawCardRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        UnoCardResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new UnoCardResponse{ Error = "It is not your turn" };
            await RespondAsync(playerId, response);
            return response;
        }
  
        if (CardsDrawn == 1)
        {
            response = new UnoCardResponse{ Error = "You can only draw 1 card, then pass if you can't play" };
            await RespondAsync(playerId, response);
            return response;
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            response = new UnoCardResponse{ Error = "No more cards" };
            await RespondAsync(playerId, response);
            return response;
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        CardsDrawn++;
        
        response = new UnoCardResponse
        {
            Card = card
        };
        await RespondAsync(playerId, response);

        await EventExtensions.InvokeOrDefault(PlayerDrewCard, () => new PlayerDrewCardNotification
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
        await EventExtensions.InvokeOrDefault(PlayerPassed, new PlayerPassedNotification
        {
            PlayerId = playerId
        });
        
        await MoveToNextPlayerOrFinishAsync();

        return response;
    }

     
    
    private PlayerViewOfGame GetPlayerViewOfGame(UnoPlayer player)
    {
        return new PlayerViewOfGame
        {
            Cards = player.Cards,
            TopOfPile = TopOfPile,
            CurrentColor = CurrentColor,
            DiscardPileCount = DiscardPile.Count,
            StockPileCount = StockPile.Count,
            OtherPlayers = Players.Where(p => p.Id != player.Id).Select(ToOtherPlayer).ToList()
        };
    }
    
    private bool TryGetCurrentPlayer(Guid playerId, [MaybeNullWhen(false)] out UnoPlayer player)
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
            await EventExtensions.InvokeOrDefault(GameEnded, () => new GameEndedNotification());
            return;
        }
        
        MoveToNextPlayer();
        await EventExtensions.InvokeOrDefault(ItsYourTurn, CurrentPlayer.Id, () => new ItsYourTurnNotification
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

    private bool CanPut(UnoCard card)
    {
        return CurrentColor == card.Color ||
               TopOfPile.Value == card.Value ||
               card.Color == UnoColor.Wild;
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

    private static OtherUnoPlayer ToOtherPlayer(UnoPlayer player)
    {
        return new OtherUnoPlayer
        {
            Name = player.Name,
            NumberOfCards = player.Cards.Count
        };
    }
    
    public override async Task StartAsync()
    {
        foreach (var player in Players)
        {
            await EventExtensions.InvokeOrDefault(GameStarted, player.Id, () => new GameStartedNotification
            {
                GameId = Id,
                PlayerViewOfGame = GetPlayerViewOfGame(player)
            });
        }
        
        await EventExtensions.InvokeOrDefault(ItsYourTurn, CurrentPlayer.Id, () => new ItsYourTurnNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(CurrentPlayer)
        });
    }
}