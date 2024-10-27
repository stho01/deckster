using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.Uno;
using Deckster.Client.Protocol;
using Deckster.Server.Collections;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.Uno.Core;

public class UnoGame : GameObject
{
    private readonly int _initialCardsPerPlayer = 7;

    public int CurrentPlayerIndex { get; set; }
    public int CardsDrawn { get; set; }
    public int GameDirection {get; set;} = 1;

    public override GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;

    public int Seed { get; set; }
    
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
    
    public async Task<DecksterResponse> PutCard(Guid playerId, UnoCard card)
    {
        IncrementSeed();
        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new FailureResponse("It is not your turn");
            await Communication.RespondAsync(playerId, response);
            return response;
        }

        if (!player.HasCard(card))
        {
            response = new FailureResponse($"You don't have '{card}'");
            await Communication.RespondAsync(playerId, response);
            return response;
        }

        if (!CanPut(card))
        {
            response = new FailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        
        if (CardsDrawn < 0)
        {
            response = new FailureResponse($"You have to draw {CardsDrawn*-1} cards");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        
        player.Cards.Remove(card);
        DiscardPile.Push(card);
        NewColor = null;
        if (!player.Cards.Any())
        {
            ScoreRound(player);
            NewRound();
            response = new UnoSuccessResponse();
            await Communication.RespondAsync(playerId, response);
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
        await Communication.RespondAsync(playerId, response);

        await Communication.NotifyAllAsync(new PlayerPutCardNotification
        {
            Card = card,
            PlayerId = playerId
        });

        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }
    
    public async Task<DecksterResponse> PutWild(Guid playerId, UnoCard card, UnoColor newColor)
    {
        IncrementSeed();

        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new FailureResponse("It is not your turn");
            await Communication.RespondAsync(playerId, response);
            return response;
        }

        if (!player.HasCard(card))
        {
            response = new FailureResponse($"You don't have '{card}'");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        
        if (card.Color != UnoColor.Wild)
        {
            response = new FailureResponse($"{card} is not a wildcard");
            await Communication.RespondAsync(playerId, response);
            return response;
        }

        if (newColor == UnoColor.Wild)
        {
            response = new FailureResponse("New color cannot be 'Wild'");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        
        if (!CanPut(card))
        {
            response = NewColor.HasValue
                ? new FailureResponse($"Cannot put '{card}' on '{TopOfPile}' (new suit: '{NewColor.Value}')")
                : new FailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
            await Communication.RespondAsync(playerId, response);
            return response;
        }

        player.Cards.Remove(card);
        DiscardPile.Push(card);
        NewColor = newColor;
        if (!player.Cards.Any())
        {
            ScoreRound(player);
            NewRound();
            response = new UnoSuccessResponse();
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        
        response = GetPlayerViewOfGame(player);
        await Communication.RespondAsync(playerId, response);
        
        await Communication.NotifyAllAsync(new PlayerPutWildNotification
        {
            PlayerId = playerId,
            Card = card,
            NewColor = newColor
        });

        await MoveToNextPlayerOrFinishAsync();
        return response;
    }
    
    
    public async Task<DecksterResponse> DrawCard(Guid playerId)
    {
        IncrementSeed();
        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new FailureResponse("It is not your turn");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
  
        if (CardsDrawn == 1)
        {
            response = new FailureResponse("You can only draw 1 card, then pass if you can't play");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            response = new FailureResponse("No more cards");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        CardsDrawn++;
        
        response = new UnoCardResponse(card);
        await Communication.RespondAsync(playerId, response);

        await Communication.NotifyAllAsync(new PlayerDrewCardNotification
        {
            PlayerId = playerId
        });
        
        if (CardsDrawn == 0) //we just paid the last penalty. Now we skip our turn
        {
            await MoveToNextPlayerOrFinishAsync();
        }
        
        return response;
    }
    
    public async Task<DecksterResponse> Pass(Guid playerId)
    {
        IncrementSeed();
        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out _))
        {
            response = new FailureResponse("It is not your turn");
            await Communication.RespondAsync(playerId, response);
            return response;
        }

        if (CardsDrawn != 1)
        {
            response = new FailureResponse("You have to draw a card first");
            await Communication.RespondAsync(playerId, response);
            return response;
        }
        
        response = new UnoSuccessResponse();
        await Communication.NotifyAllAsync(new PlayerPassedNotification
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
            await Communication.NotifyAllAsync(new GameEndedNotification());
            return;
        }
        
        MoveToNextPlayer();
        await Communication.NotifyAsync(CurrentPlayer.Id, new ItsYourTurnNotification
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

    private void IncrementSeed()
    {
        unchecked
        {
            Seed++;
        }
    }
    
    public override async Task StartAsync()
    {
        foreach (var player in Players)
        {
            await Communication.NotifyAsync(player.Id, new GameStartedNotification
            {
                GameId = Id,
                PlayerViewOfGame = GetPlayerViewOfGame(player)
            });
        }
        
        await Communication.NotifyAsync(CurrentPlayer.Id, new ItsYourTurnNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(CurrentPlayer)
        });
    }
}