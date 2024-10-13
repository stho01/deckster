
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Uno;
using Deckster.Server.Data;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.Uno.Core;

public class UnoGame: DatabaseObject
{
    private readonly int _initialCardsPerPlayer = 7;

    private int _currentPlayerIndex;
    private int _cardsDrawn;
    private int _gameDirection = 1;

    public GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;

    /// <summary>
    /// All the (shuffled) cards in the game
    /// </summary>
    public Deck Deck { get; set; } = Deck.Standard;

    /// <summary>
    /// Where players draw cards from
    /// </summary>
    public Stack<UnoCard> StockPile { get; } = new();
    
    /// <summary>
    /// Where players put cards
    /// </summary>
    public Stack<UnoCard> DiscardPile { get; } = new();

    /// <summary>
    /// All the players
    /// </summary>
    public List<UnoPlayer> Players { get; init; } = [];
 
    private UnoColor? _newColor;
    public UnoCard TopOfPile => DiscardPile.Peek();
    public UnoColor CurrentColor => _newColor ?? TopOfPile.Color;
    
    public UnoPlayer CurrentPlayer => State == GameState.Finished ? UnoPlayer.Null : Players[_currentPlayerIndex];

    public bool TryAddPlayer(Guid id, string name, [MaybeNullWhen(true)] out string reason)
    {
        if (Players.Any(p => p.Id == id))
        {
            reason = "Player already exists";
            return false;
        }
        
        if (Players.Count>=10)
        {
            reason = "Too many players";
            return false;
        }
        Players.Add(new UnoPlayer() { Id = id, Name = name });
        
        reason = default;
        return true;
    }

    public void ScoreRound(UnoPlayer winner)
    {
        winner.Score += Players.Where(x=>x.Id!=winner.Id).Sum(p => p.CalculateHandScore());
    }
    
    public void NewRound(DateTimeOffset operationTime)
    {
        foreach (var player in Players)
        {
            player.Cards.Clear();
        }
        
        _currentPlayerIndex = 0;
        StockPile.Clear();
        StockPile.PushRange(Deck.Cards);
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
    
    public UnoResponse PutCard(Guid playerId, UnoCard card)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new UnoFailureResponse("It is not your turn");
        }

        if (!player.HasCard(card))
        {
            return new UnoFailureResponse($"You don't have '{card}'");
        }

        if (!CanPut(card))
        {
            return new UnoFailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
        }
        
        if(_cardsDrawn < 0)
        {
            return new UnoFailureResponse($"You have to draw {_cardsDrawn*-1} cards");
        }
        
        player.Cards.Remove(card);
        DiscardPile.Push(card);
        _newColor = null;
        if (!player.Cards.Any())
        {
            ScoreRound(player);
            NewRound(DateTimeOffset.UtcNow);
            return new UnoSuccessResponse();
        }

        if(card.Value == UnoValue.DrawTwo)
        {
            _cardsDrawn = -2;
        }
        else if(card.Value == UnoValue.Reverse)
        {
            _gameDirection *= -1;
        }
        else if(card.Value == UnoValue.Skip)
        {
            MoveToNextPlayer();
        }
        else if(card.Value == UnoValue.WildDrawFour)
        {
            _cardsDrawn = -4;
        }
        MoveToNextPlayer();
        
        return GetPlayerViewOfGame(player);
    }
    
    public UnoResponse PutWild(Guid playerId, UnoCard card, UnoColor newColor)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new UnoFailureResponse("It is not your turn");
        }

        if (!player.HasCard(card))
        {
            return new UnoFailureResponse($"You don't have '{card}'");
        }
        
        if (card.Color != UnoColor.Wild)
        {
            return new UnoFailureResponse("Card color must be 'Wild'");
        }

        if(newColor == UnoColor.Wild)
        {
            return new UnoFailureResponse("New color cannot be 'Wild'");
        }
        
        if (!CanPut(card))
        {
            return _newColor.HasValue
                ? new UnoFailureResponse($"Cannot put '{card}' on '{TopOfPile}' (new suit: '{_newColor.Value}')")
                : new UnoFailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
        }

        player.Cards.Remove(card);
        DiscardPile.Push(card);
        _newColor = newColor;
        if (!player.Cards.Any())
        {
            ScoreRound(player);
            NewRound(DateTimeOffset.UtcNow);
            return new UnoSuccessResponse();
        }

        MoveToNextPlayer();
        
        return GetPlayerViewOfGame(player);
    }
    
    
    public UnoResponse DrawCard(Guid playerId)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new UnoFailureResponse("It is not your turn");
        }
  
        if (_cardsDrawn == 1)
        {
            return new UnoFailureResponse("You can only draw 1 card, then pass if you can't play");
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            return new UnoFailureResponse("No more cards");
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        _cardsDrawn++;
        if (_cardsDrawn == 0) //we just paid the last penalty. Now we skip our turn
        {
            MoveToNextPlayer();
        }
        return new UnoCardsResponse(card);
    }
    
    public UnoResponse Pass(Guid playerId)
    {
        if (!TryGetCurrentPlayer(playerId, out _))
        {
            return new UnoFailureResponse("It is not your turn");
        }

        if (_cardsDrawn != 1)
        {
            return new UnoFailureResponse("You have to draw a card first");
        }
        
        MoveToNextPlayer();
        return new UnoSuccessResponse();
    }
    
    private PlayerViewOfUnoGame GetPlayerViewOfGame(UnoPlayer player)
    {
        return new PlayerViewOfUnoGame
        {
            Cards = player.Cards,
            TopOfPile = TopOfPile,
            CurrentSuit = CurrentColor,
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
    private void MoveToNextPlayer()
    {
        if (Players.Count(p => p.IsStillPlaying()) < 2)
        {
            return;
        }

        var foundNext = false;
        
        var index = _currentPlayerIndex;
        while (!foundNext)
        {
            index+=_gameDirection;
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

        _currentPlayerIndex = index;
        _cardsDrawn = 0;
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
        var reshuffledCards = DiscardPile.ToList().KnuthShuffle();
        DiscardPile.Clear();
        DiscardPile.Push(topOfPile);
        StockPile.PushRange(reshuffledCards);
    }

    public PlayerViewOfUnoGame GetStateFor(Guid userId)
    {
        var player = Players.FirstOrDefault(p => p.Id == userId);
        if (player == null)
        {
            throw new Exception($"There is no player '{userId}'");
        }

        return GetPlayerViewOfGame(player);
    }

    private static OtherUnoPlayer ToOtherPlayer(UnoPlayer player)
    {
        return new OtherUnoPlayer
        {
            Name = player.Name,
            NumberOfCards = player.Cards.Count
        };
    }

    public void RemovePlayer(Guid id)
    {
        Players.RemoveAll(p => p.Id == id);
    }

    private readonly object _lock = new object();
    
    public bool ContainsPlayer(Guid userId)
    {
        lock (_lock)
        {
            return Players.Any(p => p.Id == userId);
        }
    }
    
}