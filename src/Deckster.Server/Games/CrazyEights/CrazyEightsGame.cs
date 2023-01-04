using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGame
{
    private readonly int _initialCardsPerPlayer;
    private const int DefaultInitialCardsPerPlayer = 5;
    
    public List<CrazyEightsPlayer> DonePlayers { get; } = new();
    private int _currentPlayerIndex;
    private int _cardsDrawn;
    
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;
    
    /// <summary>
    /// All the (shuffled) cards in the game
    /// </summary>
    public Deck Deck { get; }

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
    public CrazyEightsPlayer[] Players { get; }

    private Suit? _newSuit;
    public Card TopOfPile => DiscardPile.Peek();
    public Suit CurrentSuit => _newSuit ?? TopOfPile.Suit;

    public CrazyEightsPlayer CurrentPlayer => State == GameState.Finished ? CrazyEightsPlayer.Null : Players[_currentPlayerIndex];

    public CrazyEightsGame(Deck deck, CrazyEightsPlayer[] players) : this(deck, players, DefaultInitialCardsPerPlayer)
    {
    }
    
    public CrazyEightsGame(Deck deck, CrazyEightsPlayer[] players, int initialCardsPerPlayer)
    {
        Deck = deck;
        Players = players;
        _initialCardsPerPlayer = initialCardsPerPlayer;
        if (Deck.Cards.Count <= players.Length * initialCardsPerPlayer)
        {
            throw new ArgumentException($"Not enough cards in deck ({Deck.Cards.Count})", nameof(initialCardsPerPlayer));
        }
        Reset();
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

    public CommandResult PutCard(Guid playerId, Card card)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new FailureResult("It is not your turn");
        }

        if (!player.HasCard(card))
        {
            return new FailureResult($"You don't have '{card}'");
        }

        if (!CanPut(card))
        {
            return new FailureResult($"Cannot put '{card}' on '{TopOfPile}'");
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

    public CommandResult PutEight(Guid playerId, Card card, Suit newSuit)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new FailureResult("It is not your turn");
        }

        if (!player.HasCard(card))
        {
            return new FailureResult($"You don't have '{card}'");
        }
        
        if (card.Rank != 8)
        {
            return new FailureResult("Card rank must be '8'");
        }

        if (!CanPut(card))
        {
            return _newSuit.HasValue
                ? new FailureResult($"Cannot put '{card}' on '{TopOfPile}' (new suit: '{_newSuit.Value}')")
                : new FailureResult($"Cannot put '{card}' on '{TopOfPile}'");
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
    
    public CommandResult DrawCard(Guid playerId)
    {
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            return new FailureResult("It is not your turn");
        }
        
        if (_cardsDrawn > 2)
        {
            return new FailureResult("You can only draw 3 cards");
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            return new FailureResult("No more cards");
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        _cardsDrawn++;
        
        return new CardResult(card);
    }
    
    public CommandResult Pass(Guid playerId)
    {
        if (!TryGetCurrentPlayer(playerId, out _))
        {
            return new FailureResult("It is not your turn");
        }
        
        MoveToNextPlayer();
        return new SuccessResult();
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
            if (index >= Players.Length)
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
}