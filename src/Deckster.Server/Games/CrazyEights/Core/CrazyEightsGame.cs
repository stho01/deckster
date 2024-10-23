using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Deckster.Client.Common;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Protocol;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.CrazyEights.Core;

public class CrazyEightsGame : GameObject
{
    private static readonly Dictionary<Type, MethodInfo> _applies;
    
    static CrazyEightsGame()
    {
        var methods = from method in typeof(CrazyEightsGame)
                .GetMethods()
            where method.Name == "Apply" && method.ReturnType == typeof(Task)
            let parameters = method.GetParameters()
            where parameters.Length == 1 && parameters[0].ParameterType.IsSubclassOf(typeof(DecksterRequest))
            let parameter = parameters[0]
            select (parameter, method);
        _applies = methods.ToDictionary(p => p.parameter.ParameterType,
            p => p.method);
    }
    
    private ICommunicationContext _context = NullContext.Instance;
    
    // ReSharper disable once UnusedMember.Global
    // Used by Marten
    public int Seed { get; set; }
    
    private readonly int _initialCardsPerPlayer = 5;
    
    public List<CrazyEightsPlayer> DonePlayers { get; } = [];
    private int _currentPlayerIndex;
    private int _cardsDrawn;
    
    public GameState State => Players.Count(p => p.IsStillPlaying()) > 1 ? GameState.Running : GameState.Finished;

    /// <summary>
    /// All the (shuffled) cards in the game
    /// </summary>
    public List<Card> Deck { get; init; } = [];

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

    private CrazyEightsGame()
    {
        
    }

    public static CrazyEightsGame Create(CrazyEightsGameCreatedEvent created)
    {
        var game = new CrazyEightsGame
        {
            Id = created.Id,
            Players = created.Players.Select(p => new CrazyEightsPlayer
            {
                Id = p.Id,
                Name = p.Name
            }).ToList(),
            Deck = created.Deck,
            Seed = created.InitialSeed,
            _context = created.GetContext()
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
        
        _currentPlayerIndex = 0;
        DonePlayers.Clear();
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
        DonePlayers.Clear();
    }

    public Task HandleAsync(DecksterRequest request)
    {
        if (!_applies.TryGetValue(request.GetType(), out var del))
        {
            return _context.RespondAsync(request.PlayerId, new FailureResponse($"Unsupported request: '{request.GetType().Name}'"));
        }

        return (Task) del.Invoke(this, new object?[]{request});
    }
    

    public Task Apply(PutCardRequest @event) => PutCard(@event.PlayerId, @event.Card);

    public async Task<DecksterResponse> PutCard(Guid playerId, Card card)
    {
        IncrementSeed();
        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new FailureResponse("It is not your turn");
            await _context.RespondAsync(playerId, response);
            return response;
        }

        if (!player.HasCard(card))
        {
            response = new FailureResponse($"You don't have '{card}'");
            await _context.RespondAsync(playerId, response);
            return response;
        }

        if (!CanPut(card))
        {
            response = new FailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
            await _context.RespondAsync(playerId, response);
            return response;
        }
        
        player.Cards.Remove(card);
        DiscardPile.Push(card);
        _newSuit = null;
        if (!player.Cards.Any())
        {
            DonePlayers.Add(player);
        }

        response = GetPlayerViewOfGame(player);
        await _context.RespondAsync(playerId, response);

        await MoveToNextPlayerOrFinishAsync();
        
        return response;
    }

    public Task Apply(PutEightRequest @event) => PutEight(@event.PlayerId, @event.Card, @event.NewSuit);

    public async Task<DecksterResponse> PutEight(Guid playerId, Card card, Suit newSuit)
    {
        IncrementSeed();
        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new FailureResponse("It is not your turn");
            await _context.RespondAsync(playerId, response);
            return response;
        }

        if (!player.HasCard(card))
        {
            response = new FailureResponse($"You don't have '{card}'");
            await _context.RespondAsync(playerId, response);
            return response;
        }
        
        if (card.Rank != 8)
        {
            response = new FailureResponse("Card rank must be '8'");
            await _context.RespondAsync(playerId, response);
            return response;
        }

        if (!CanPut(card))
        {
            response = _newSuit.HasValue
                ? new FailureResponse($"Cannot put '{card}' on '{TopOfPile}' (new suit: '{_newSuit.Value}')")
                : new FailureResponse($"Cannot put '{card}' on '{TopOfPile}'");
            await _context.RespondAsync(playerId, response);
            return response;
        }

        player.Cards.Remove(card);
        DiscardPile.Push(card);
        _newSuit = newSuit != card.Suit ? newSuit : null;
        
        response = GetPlayerViewOfGame(player);
        await _context.RespondAsync(playerId, response);
        
        if (!player.Cards.Any())
        {
            DonePlayers.Add(player);
            await _context.NotifyAllAsync(new PlayerIsDoneNotification
            {
                PlayerId = playerId
            });
        }

        await MoveToNextPlayerOrFinishAsync();
        return response;
    }

    public Task Apply(DrawCardRequest @event) => DrawCard(@event.PlayerId);

    public async Task<DecksterResponse> DrawCard(Guid playerId)
    {
        IncrementSeed();
        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new FailureResponse("It is not your turn"); 
            await _context.RespondAsync(playerId, response);
            return response;
        }
        
        if (_cardsDrawn > 2)
        {
            response = new FailureResponse("You can only draw 3 cards");
            await _context.RespondAsync(playerId, response);
            return response;
        }
        
        ShufflePileIfNecessary();
        if (!StockPile.Any())
        {
            response = new FailureResponse("Stock pile is empty");
            await _context.RespondAsync(playerId, response);
            return response;
        }
        var card = StockPile.Pop();
        player.Cards.Add(card);
        _cardsDrawn++;
        
        response = new CardResponse(card);
        await _context.RespondAsync(playerId, response);

        await _context.NotifyAllAsync(new PlayerDrewCardNotification
        {
            PlayerId = playerId
        });
        return response;
    }

    public Task Apply(PassRequest @event) => Pass(@event.PlayerId);

    public async Task<DecksterResponse> Pass(Guid playerId)
    {
        IncrementSeed();
        DecksterResponse response;
        if (!TryGetCurrentPlayer(playerId, out _))
        {
            response = new FailureResponse("It is not your turn");
            await _context.RespondAsync(playerId, response);
            return response;
        }
        
        response = new PassOkResponse();
        await _context.RespondAsync(playerId, response);
        
        await _context.NotifyAllAsync(new PlayerPassedNotification
        {
            PlayerId = playerId
        });
        await MoveToNextPlayerOrFinishAsync();
        return response;
    }

    private void IncrementSeed()
    {
        unchecked
        {
            Seed++;
        }
    }
    
    private async Task MoveToNextPlayerOrFinishAsync()
    {
        if (State == GameState.Finished)
        {
            await _context.NotifyAllAsync(new GameEndedNotification());
            return;
        }
        
        MoveToNextPlayer();
        await _context.NotifyAsync(CurrentPlayer.Id, new ItsYourTurnNotification
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

    public Task StartAsync()
    {
        return _context.NotifyAsync(CurrentPlayer.Id, new ItsYourTurnNotification
        {
            PlayerViewOfGame = GetPlayerViewOfGame(CurrentPlayer)
        });
    }
}
