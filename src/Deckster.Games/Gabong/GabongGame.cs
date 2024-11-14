using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Gabong;

namespace Deckster.Games.Gabong;

public class GabongGame : GameObject
{
    public event NotifyAll<GameStartedNotification>? GameStarted;
    public event NotifyAll<PlayerPutCardNotification>? PlayerPutCard;
    public event NotifyAll<PlayerDrewCardNotification>? PlayerDrewCard;
    public event NotifyAll<PlayerDrewPenaltyCardNotification>? PlayerDrewPenaltyCard;
    public event NotifyAll<GameEndedNotification>? GameEnded;
    public event NotifyPlayer<RoundStartedNotification>? RoundStarted;
    public event NotifyAll<RoundEndedNotification>? RoundEnded;
    public event NotifyAll<PlayerLostTheirTurnNotification>? PlayerLostTheirTurn;
    public event NotifySelf<PlayerTookTooLongNotification>? PlayerTookTooLong;
    public event NotifySelf<PlayerHasToomanyCardsNotification>? PlayerHasTooManyCards;

    

    public int CardsToDraw { get; set; }
    public int CardsDrawn { get; set; }
    public int GameDirection { get; set; } = 1;

    public const int INITIAL_HAND_SIZE = 7;
    public const int MAX_CARDS_IN_HAND = 20;
    public const int MAX_SECONDS_PER_TURN = 5;
    public const int MAX_SECONDS_BEFORE_STARTING_ROUND = 60;
    
    public Guid GabongMasterId { get; set; } = Guid.Empty;

    public int LastPlayMadeByPlayerIndex
    {
        get => _lastPlayMadeByPlayerIndex;
        set
        {
            _lastPlayMadeAt = DateTime.UtcNow;
            _lastPlayMadeByPlayerIndex = value;
        }
    }

    protected override GameState GetState() => 
        Players.Any(p => p.Score >= 100) ? GameState.Finished 
            : Players.Any(p=>p.Cards.Count==0) ? GameState.RoundFinished 
            : GameState.Running;

    public bool IsBetweenRounds { get; set; } = true;

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

    public Suit? NewSuit { get; set; }
    public Card TopOfPile => DiscardPile.PeekOrDefault();
    private GabongPlay LastPlay { get; set; } = GabongPlay.RoundStarted;
    public Suit CurrentSuit => NewSuit ?? TopOfPile.Suit;

    public GabongPlayer CurrentPlayer => CalculateCurrentPlayer();

    private GabongPlayer CalculateCurrentPlayer()
    {
        if(State == GameState.Waiting)
        {
            return GabongPlayer.Null;
        } 
        if(State == GameState.Finished)
        {
            return GabongPlayer.Null;
        }
        if(LastPlay == GabongPlay.RoundStarted)
        {
            return PlayerIndexAdjustedBy(0);
        }
        if(LastPlay == GabongPlay.CardPlayed)
        {
            var adjust = DiscardPile.TryPeek(out var top)
                ? top.Rank == 3 ? 2 : 1
                : 1;
            return PlayerIndexAdjustedBy(adjust);
        }
        return PlayerIndexAdjustedBy(1);
    }

    private GabongPlayer PlayerIndexAdjustedBy(int delta)
    {
        return Players[ (Players.Count + LastPlayMadeByPlayerIndex + (delta * GameDirection)) % Players.Count];
    }

    public static GabongGame Create(GabongGameCreatedEvent created)
    {
        var game = new GabongGame
        {
            Id = created.Id,
            StartedTime = created.StartedTime,
            IsBetweenRounds = true,
            Players = created.Players.Select(p => new GabongPlayer
            {
                Id = p.Id,
                Name = p.Name
            }).ToList(),
            Deck = created.Deck,
            Seed = created.InitialSeed,
        };
        return game;
    }

   

    private void NewRound()
    {
        IsBetweenRounds = true;
        IncrementSeed();
        foreach (var player in Players)
        {
            player.Cards.Clear();
        }
       
        LastPlay = GabongPlay.RoundStarted;
        LastPlayMadeByPlayerIndex = GetPlayerIndex(GabongMasterId);
        
        StockPile.Clear();
        StockPile.PushRange(Deck);
        for (var ii = 0; ii < INITIAL_HAND_SIZE; ii++)
        {
            foreach (var player in Players)
            {
                player.Cards.Add(StockPile.Pop());
            }
        }

        DiscardPile.Clear();
        var startingCard = StockPile.Pop();
        var toReshuffle = new List<Card>();
        while (startingCard.IsASpecialCard()) //we don't want to start with a special card
        {
            toReshuffle.Add(startingCard);
            startingCard = StockPile.Pop();
        }
        DiscardPile.Push(startingCard);
        StockPile.PushRange(toReshuffle);
        ShufflePileIfNecessary();
        IsBetweenRounds = false;
    }

    private int GetPlayerIndex(Guid lastGabongMadeBy)
    {
        return Players.FindIndex(p => p.Id == lastGabongMadeBy);
    }

    public async Task<PlayerViewOfGame> PutCard(PutCardRequest request)
    {
        IncrementSeed();

        var card = request.Card;
        TryGetPlayer(request.PlayerId, out var player);
        if (player == null)
        {
            var wtf =  new PlayerViewOfGame($"You don't exist");
            await RespondAsync(request.PlayerId, wtf);
            return wtf;
        }
        
        if (!player.HasCard(card))
        {
            return await PenalizePlayer(player, 1, $"NO! You don't have '{card}'");
        }
      
        if(CurrentPlayer != player && !card.Equals(TopOfPile))
        {
            return await PenalizePlayer(player, 1, "NO! It is not your turn");
        }
        if(CurrentPlayer != player && card.Equals(TopOfPile))
        {
            player.Shots++;
        }
      
        if (!CanPut(card))
        {
            return await PenalizePlayer(player, 1,$"NO! Cannot put '{card}' on '{TopOfPile}'");
        }

        if (card.Rank != 8 && request.NewSuit.HasValue)
        {
            return await PenalizePlayer(player, 1,$"NO! Cannot change suit with a '{card}'");
        }

        if (CardsToDraw > 0 && card.Rank != 2)
        {
            return await PenalizePlayer(player, 1,$"NO! You have to draw {Math.Abs(CardsToDraw)} more cards");
        }

        player.CardsPlayed++;
        player.Cards.Remove(card);
        return await HandleMaybeRoundEnded()
               ?? await HandlePlay(card, player, null);
    }

    private async Task<PlayerViewOfGame?> HandleMaybeRoundEnded(bool outsideFactorsSaysItsFinished = false)
    {
        if (State == GameState.RoundFinished || outsideFactorsSaysItsFinished)
        {
            
            await EndRound();
            if(State == GameState.Finished)
            {
                await GameEnded.InvokeOrDefault(() => new GameEndedNotification
                {
                    Players = Players.Select(p => p.ToPlayerData()).ToList()
                });
                return new PlayerViewOfGame("Game Over");
            }
            else
            {
                await RoundEnded.InvokeOrDefault(() => new RoundEndedNotification
                {
                    Players = Players.Select(p => p.ToPlayerData()).ToList()
                });
                await StartNewRound();
            }
            return new PlayerViewOfGame("New Round Started");
        }
        return null;
    }

    private async Task<PlayerViewOfGame> HandlePlay(Card card, GabongPlayer player, Suit? newSuit)
    {
        DiscardPile.Push(card);
        LastPlayMadeByPlayerIndex = Players.IndexOf(player);
        LastPlay = GabongPlay.CardPlayed;
        NewSuit = newSuit;
  
        if (card.Rank == 2)
        {
            CardsToDraw += 2;
        }
        else
        {
            CardsToDraw = 0;
        }
        if (card.Rank == 13)
        {
            GameDirection *= -1;
        }
        
        var response = GetPlayerViewOfGame(player);
        await RespondAsync(player.Id, response);

        await PlayerPutCard.InvokeOrDefault(() => new PlayerPutCardNotification
        {
            Card = card,
            PlayerId = player.Id,
            NewSuit = newSuit
        });

        return response;
        
    }

    private async Task StartNewRound()
    {
        NewRound();
        foreach (var player in Players)
        {
            await RoundStarted.InvokeOrDefault(player.Id, () => new RoundStartedNotification
            {
                PlayerViewOfGame = GetPlayerViewOfGame(player),
                StartingPlayerId = Players[LastPlayMadeByPlayerIndex].Id
            });
        }
    }

    private async Task EndRound()
    {
        IsBetweenRounds = true;
        Players.ForEach(p=> p.ScoreRound());
    }

    private async Task<PlayerViewOfGame> PenalizePlayer(GabongPlayer player, int amount, string message, PlayerLostTurnReason reason = PlayerLostTurnReason.WrongPlay)
    {
        var playerIndex = Players.IndexOf(player);
        for(var i = 0; i<amount; i++)
        {
            player.Cards.Add(StockPile.Pop());
            await PlayerDrewPenaltyCard.InvokeOrDefault(() => new PlayerDrewPenaltyCardNotification { PlayerId = player.Id });
            player.Penalties++;
        }
        
        if (CurrentPlayer == player)
        {
            LastPlay = GabongPlay.TurnLost;
            LastPlayMadeByPlayerIndex = playerIndex;
            await PlayerLostTheirTurn.InvokeOrDefault(() => new PlayerLostTheirTurnNotification
            {
                PlayerId = player.Id,
                LostTurnReason = reason
            });
            player.TurnsLost++;
        }

        if (player.Cards.Count > MAX_CARDS_IN_HAND)
        {
            await PlayerHasTooManyCards.InvokeOrDefault(() => new PlayerHasToomanyCardsNotification { PlayerId = player.Id });
        }
        var response = GetPlayerViewOfGame(player, message);
        await RespondAsync(player.Id, response);
        return response;
    }

    public async Task<PlayerViewOfGame> DrawCard(DrawCardRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        var player = ResolvePlayerById(request.PlayerId);
        if (player == null)
        {
            return new PlayerViewOfGame("You don't exist");
        }

        ShufflePileIfNecessary();

        var card = StockPile.Pop();
        player.Cards.Add(card);
        var drawnCard = StockPile.Pop();
        player.Cards.Add(drawnCard);
        if (CardsToDraw == 0)
        {
            CardsDrawn++;
        }
        if (CurrentPlayer.Id == playerId && CardsToDraw > 0)
        {
            CardsToDraw--;
            player.DebtDrawn++;
            if(CardsToDraw == 0)
            {
                LastPlay = GabongPlay.TurnLost;
                LastPlayMadeByPlayerIndex = Players.IndexOf(player);
                await PlayerLostTheirTurn.InvokeOrDefault(() => new PlayerLostTheirTurnNotification()
                {
                    PlayerId = playerId,
                    LostTurnReason = PlayerLostTurnReason.FinishedDrawingCardDebt
                });
            }
        }
        await PlayerDrewCard.InvokeOrDefault(() => new PlayerDrewCardNotification
        {
            PlayerId = playerId
        });

        
        var response = GetPlayerViewOfGame(player).WithCardsAddedNotification(drawnCard);
        await RespondAsync(playerId, response);
        
        if (player.Cards.Count > MAX_CARDS_IN_HAND)
        {
            await PlayerHasTooManyCards.InvokeOrDefault(() => new PlayerHasToomanyCardsNotification { PlayerId = player.Id });
        }
        return response;
    }

    private GabongPlayer? ResolvePlayerById(Guid playerId)
    {
        return Players.FirstOrDefault(x=>x.Id==playerId);
    }


    public async Task<PlayerViewOfGame> Pass(PassRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            var errorResponse = new PlayerViewOfGame("It is not your turn");
            await RespondAsync(playerId, errorResponse);
            return errorResponse;
        }

        if (CardsDrawn == 0)
        {
            var errorResponse = await PenalizePlayer(player, 1, "You have to draw a card first");
            return errorResponse;
        }

        await PlayerLostTheirTurn.InvokeOrDefault(() => new PlayerLostTheirTurnNotification()
        {
            PlayerId = playerId,
            LostTurnReason = PlayerLostTurnReason.Passed
        });
        player.Passes++;
        var okResponse = GetPlayerViewOfGame(player);
        await RespondAsync(playerId, okResponse);
        return okResponse;
    }


    private PlayerViewOfGame GetPlayerViewOfGame(GabongPlayer player, string errorString = null)
    {
        return new PlayerViewOfGame
        {
            Error = errorString,
            Cards = player.Cards,
            TopOfPile = TopOfPile,
            CurrentSuit = CurrentSuit,
            DiscardPileCount = DiscardPile.Count,
            RoundStarted = !IsBetweenRounds,
            StockPileCount = StockPile.Count,
            Players = Players.Select(ToSlimPlayer).ToList(),
            LastPlayMadeByPlayerId = Players[LastPlayMadeByPlayerIndex].Id,
            LastPlay = LastPlay,
            PlayersOrder = Players.Select(x=>x.Id).ToList(),
            CardDebtToDraw = CardsToDraw,
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

    private bool TryGetPlayer(Guid playerId, [MaybeNullWhen(false)] out GabongPlayer player)
    {
        player = Players.FirstOrDefault(x => x.Id == playerId);
        return player != null;
    }

    private bool CanPut(Card card)
    {
        return CurrentSuit == card.Suit ||
               TopOfPile.Rank == card.Rank ||
               card.Rank == 8;
    }

    private void ShufflePileIfNecessary()
    {
        if (StockPile.Count > 3)
        {
            return;
        }

        if (DiscardPile.Count < 2)
        {
            return;
        }
        ShufflePile(14);
    }

    private void ShufflePile(int saveTopCards)
    {
        saveTopCards = Math.Min(saveTopCards, DiscardPile.Count);
        var saved = new List<Card>();
        for (int i = 0; i < saveTopCards; i++) //save the top 14 cards
        {
            saved.Push(DiscardPile.Pop());
        }
        
        var reshuffledCards = DiscardPile.KnuthShuffle(Seed);
        DiscardPile.Clear();
        for (int i = 0; i < saveTopCards; i++) //save the top 14 cards
        {
            DiscardPile.Push(saved.Pop());
        }
        StockPile.PushRange(reshuffledCards);
    }

    private static SlimGabongPlayer ToSlimPlayer(GabongPlayer player)
    {
        return new SlimGabongPlayer
        {
            Id = player.Id,
            Name = player.Name,
            NumberOfCards = player.Cards.Count
        };
    }

    private CancellationTokenSource _playIsOngoing = new();
    private Task _clockTask = Task.CompletedTask;
    private DateTime _lastPlayMadeAt = DateTime.UtcNow;
    private int _lastPlayMadeByPlayerIndex;

    public override async Task StartAsync()
    {
        _playIsOngoing = new CancellationTokenSource();
        _lastPlayMadeAt = DateTime.UtcNow;
        _clockTask = Task.Run(async () =>
        {
            while (!_playIsOngoing.Token.IsCancellationRequested)
            {
                await Task.Delay(1000);
                if(IsBetweenRounds)
                {
                    continue;
                }
                if(State == GameState.Running && LastPlay == GabongPlay.RoundStarted && _lastPlayMadeAt < DateTime.UtcNow.AddSeconds(-MAX_SECONDS_BEFORE_STARTING_ROUND))
                {
                   await PlayerTookTooLong.InvokeOrDefault(()=>new PlayerTookTooLongNotification { PlayerId = CurrentPlayer.Id });
                }
                else if (State == GameState.Running && _lastPlayMadeAt < DateTime.UtcNow.AddSeconds(-MAX_SECONDS_PER_TURN))
                {
                    await PlayerTookTooLong.InvokeOrDefault(()=>new PlayerTookTooLongNotification { PlayerId = CurrentPlayer.Id });
                }
            }
        });
        await GameStarted.InvokeOrDefault(() => new GameStartedNotification { GameId = Id, });
        await PickFirstGabongMaster();
        await StartNewRound();
    }

    private async Task PickFirstGabongMaster()
    {
        //only ever run once on game start - starting player will only change on "Gabong"
        IncrementSeed();
        var random = new Random(Seed);
        var firstGabongMasterIndex = random.Next(Players.Count);
        GabongMasterId = Players[firstGabongMasterIndex].Id;
    }

    public async Task<PlayerViewOfGame> PlayGabong(PlayGabongRequest request)
    {
        var playerId = request.PlayerId;
        var player = ResolvePlayerById(playerId);
        if(player == null)
        {
            return new PlayerViewOfGame("You don't exist");
        }

        if (GabongCalculator.IsGabong(TopOfPile.Rank, player.Cards.Select(x=>x.Rank)))
        {
            player.Gabongs++;
            player.Score -= 5;
            player.Cards.Clear();
            GabongMasterId = playerId;
            return await HandleMaybeRoundEnded() 
                   ?? new PlayerViewOfGame("Round ended");
        }
        else
        {
            return await PenalizePlayer(player, 2, "NO! You don't have Gabong");
        }
    }  
    
    public async Task<PlayerViewOfGame> PlayBonga(PlayBongaRequest request)
    {
        var playerId = request.PlayerId;
        var player = ResolvePlayerById(playerId);
        if(player == null)
        {
            return new PlayerViewOfGame("You don't exist");
        }

        int i = 1;
        bool goOn = true;
        bool success = false;
        while (!success && goOn && i < DiscardPile.Count)
        {
            var target = DiscardPile.Take(i).Sum(x=>x.Rank);
            if (target > 14)
            {
                goOn = false;
            }
            success = GabongCalculator.IsGabong(target, player.Cards.Select(x=>x.Rank));
        }
        if (success)
        {
            player.Bongas++;
            player.Score -= 5;
            player.Cards.Clear();
            return await HandleMaybeRoundEnded() 
                   ?? new PlayerViewOfGame("Round ended");
        }
        else
        {
            return await PenalizePlayer(player, 2, "NO! You don't have bonga");
        }
    }

    public Task ReceiveSelfNotification(GabongGameSelfNotification gabongNotification)
    {
        if(gabongNotification is PlayerTookTooLongNotification tookTooLong && State == GameState.Running && CurrentPlayer.Id == tookTooLong.PlayerId)
        {
            return PenalizePlayer(ResolvePlayerById(tookTooLong.PlayerId), 1, "You took too long to play", PlayerLostTurnReason.TookTooLong);
        }
        if(gabongNotification is PlayerHasToomanyCardsNotification toomanyCardsNotification )
        {
            var player = Players.FirstOrDefault(x => x.Id == toomanyCardsNotification.PlayerId);
            if (player != null && State == GameState.Running && player.Cards.Count > MAX_CARDS_IN_HAND)
            {
                return HandleMaybeRoundEnded(true);               
            }
        }
        return Task.CompletedTask;
    }
}