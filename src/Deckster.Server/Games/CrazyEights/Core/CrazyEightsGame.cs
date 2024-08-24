using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Protocol;
using Deckster.Server.Data;

namespace Deckster.Server.Games.CrazyEights.Core;

public class CrazyEightsGame : DatabaseObject
{
    private readonly int _initialCardsPerPlayer = 5;
    
    public List<CrazyEightsPlayer> DonePlayers { get; } = new();
    private int _currentPlayerIndex;
    private int _cardsDrawn;
    
    public GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;
    
    /// <summary>
    /// All the (shuffled) cards in the game
    /// </summary>
    public Deck Deck { get; set; } = Deck.Standard;

    /// <summary>
    /// Where players draw cards from
    /// </summary>
    public Stack<Card> StockPile { get; } = new();
    
    /// <summary>
    /// Where players put cards
    /// </summary>
    public Stack<Card> DiscardPile { get; } = new();

    /// <summary>
    /// All the players
    /// </summary>
    public List<CrazyEightsPlayer> Players { get; init; } = [];

    private Suit? _newSuit;
    public Card TopOfPile => DiscardPile.Peek();
    public Suit CurrentSuit => _newSuit ?? TopOfPile.Suit;

    public CrazyEightsPlayer CurrentPlayer => State == GameState.Finished ? CrazyEightsPlayer.Null : Players[_currentPlayerIndex];

    public bool TryAddPlayer(Guid id, string name, [MaybeNullWhen(true)] out string reason)
    {
        if (Players.Any(p => p.Id == id))
        {
            reason = "Player already exists";
            return false;
        }
        
        if (Deck.Cards.Count <= (Players.Count + 1) * _initialCardsPerPlayer)
        {
            reason = "Too many players";
            return false;
        }
        Players.Add(new CrazyEightsPlayer { Id = id, Name = name });
        
        reason = default;
        return true;
    }

    public void Reset()
    {
        foreach (var player in Players)
        {
            player.Cards.Clear();
        }
        
        _currentPlayerIndex = 0;
        DonePlayers.Clear();
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
        DonePlayers.Clear();
    }

    public DecksterResponse PutCard(Guid playerId, Card card)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new FailureResponse("It is not your turn");
        }

        if (!player.HasCard(card))
        {
            return new FailureResponse($"You don't have '{card}'");
        }

        if (!CanPut(card))
        {
            return new FailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
        }
        
        player.Cards.Remove(card);
        DiscardPile.Push(card);
        _newSuit = null;
        if (!player.Cards.Any())
        {
            DonePlayers.Add(player);
        }

        MoveToNextPlayer();
        
        return GetPlayerViewOfGame(player);
    }

    public DecksterResponse PutEight(Guid playerId, Card card, Suit newSuit)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new FailureResponse("It is not your turn");
        }

        if (!player.HasCard(card))
        {
            return new FailureResponse($"You don't have '{card}'");
        }
        
        if (card.Rank != 8)
        {
            return new FailureResponse("Card rank must be '8'");
        }

        if (!CanPut(card))
        {
            return _newSuit.HasValue
                ? new FailureResponse($"Cannot put '{card}' on '{TopOfPile}' (new suit: '{_newSuit.Value}')")
                : new FailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
        }

        player.Cards.Remove(card);
        DiscardPile.Push(card);
        _newSuit = newSuit != card.Suit ? newSuit : null;
        if (!player.Cards.Any())
        {
            DonePlayers.Add(player);
        }

        MoveToNextPlayer();
        
        return GetPlayerViewOfGame(player);
    }
    
    public DecksterResponse DrawCard(Guid playerId)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new FailureResponse("It is not your turn");
        }
        
        if (_cardsDrawn > 2)
        {
            return new FailureResponse("You can only draw 3 cards");
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            return new FailureResponse("No more cards");
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        _cardsDrawn++;
        
        return new CardResponse(card);
    }
    
    public DecksterResponse Pass(Guid playerId)
    {
        if (!TryGetCurrentPlayer(playerId, out _))
        {
            return new FailureResponse("It is not your turn");
        }
        
        MoveToNextPlayer();
        return new SuccessResponse();
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
        
        var index = _currentPlayerIndex;
        while (!foundNext)
        {
            index++;
            if (index >= Players.Count)
            {
                index = 0;
            }

            foundNext = Players[index].IsStillPlaying();
        }

        _currentPlayerIndex = index;
        _cardsDrawn = 0;
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
        var reshuffledCards = DiscardPile.ToList().KnuthShuffle();
        DiscardPile.Clear();
        DiscardPile.Push(topOfPile);
        StockPile.PushRange(reshuffledCards);
    }

    public PlayerViewOfGame GetStateFor(Guid userId)
    {
        var player = Players.FirstOrDefault(p => p.Id == userId);
        if (player == null)
        {
            throw new Exception($"There is no player '{userId}'");
        }

        return GetPlayerViewOfGame(player);
    }

    private static OtherCrazyEightsPlayer ToOtherPlayer(CrazyEightsPlayer player)
    {
        return new OtherCrazyEightsPlayer
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