using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Yaniv;
using Deckster.Games.Collections;

namespace Deckster.Games.Yaniv;

public class YanivGame : GameObject
{
    private const int PointLimit = 100;
    public const int Penalty = 30;
    
    public event NotifyAll<PlayerPutCardsNotification> PlayerPutCards;
    public event NotifyPlayer<ItsYourTurnNotification> ItsYourTurn;
    public event NotifyPlayer<RoundStartedNotification> RoundStarted;
    public event NotifyAll<RoundEndedNotification> RoundEnded;
    public event NotifyAll<GameEndedNotification> GameEnded;
    
    public event NotifyAll<DiscardPileShuffledNotification> DiscardPileShuffled;

    protected override GameState GetState() => GameOver ? GameState.Finished : GameState.Running;
    
    public bool GameOver { get; set; }
    
    public List<YanivPlayer> Players { get; init; } = [];
    
    public List<Card> Deck { get; init; } = [];
    public List<Card> StockPile { get; init; } = [];
    public List<Card> DiscardPile { get; init; } = [];
    public Card? TopOfPile => DiscardPile.Peek();
    
    public YanivPlayer CurrentPlayer => State == GameState.Finished ? YanivPlayer.Null : Players[CurrentPlayerIndex];
    public int CurrentPlayerIndex { get; set; }

    public static YanivGame Create(YanivGameCreatedEvent created)
    {
        var game = new YanivGame
        {
            Id = created.Id,
            StartedTime = created.StartedTime,
            Seed = created.InitialSeed,
            Deck = created.Deck,
            Players = created.Players.Select(p => new YanivPlayer
            {
                Id = p.Id,
                Name = p.Name,
                Points = 0,
            }).ToList()
        };
        
        return game;
    }

    public void Deal()
    {
        StockPile.Clear();
        DiscardPile.Clear();
        StockPile.PushRange(Deck);
        
        foreach (var player in Players)
        {
            player.CardsOnHand.Clear();
        }
        
        for (var ii = 0; ii < 5; ii++)
        {
            foreach (var player in Players)
            {
                player.CardsOnHand.Add(StockPile.Pop());
            }
        }

        DiscardPile.Push(StockPile.Pop());
        CurrentPlayerIndex = new Random(Seed).Next(0, Players.Count);
    }

    public async Task<EmptyResponse> CallYaniv(CallYanivRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        EmptyResponse response;
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new EmptyResponse {Error = "It is not your turn"};
            await RespondAsync(playerId, response);
            return response;
        }

        if (player.SumOnHand > 5)
        {
            response = new EmptyResponse {Error = "You must have 5 points or less on hand"};
            await RespondAsync(playerId, response);
            return response;
        }

        response = new EmptyResponse();
        await RespondAsync(playerId, response);

        var scores = Players.Select(p => new PlayerRoundScore
        {
            PlayerId = p.Id,
            Cards = p.CardsOnHand.ToArray(),
            Points = p.SumOnHand
        }).ToList();

        var playerScore = scores.Single(s => s.PlayerId == playerId);

        if (scores.Any(s => s.PlayerId != playerId && s.Points <= playerScore.Points))
        {
            playerScore.Penalty = Penalty;
        }
        else
        {
            playerScore.Points = 0;
        }

        foreach (var score in scores)
        {
            var p = Players.Single(p => p.Id == score.PlayerId);
            p.Points += score.Points;
            p.Penalty += score.Penalty;
        }

        await RoundEnded.InvokeOrDefault(() => new RoundEndedNotification
        {
            WinnerPlayerId = scores.OrderBy(s => s.TotalPoints).First().PlayerId,
            PlayerScores = scores.ToArray()
        });

        if (Players.Any(p => p.TotalPoints >= PointLimit))
        {
            var gameScores = Players
                .Select(p => new PlayerFinalScore
            {
                PlayerId = p.Id,
                Points = p.Points,
                Penalty = p.Penalty,
                FinalPoints = p.TotalPoints == PointLimit ? 0 : p.TotalPoints
            })
                .OrderBy(s => s.FinalPoints)
                .ToArray();

            GameOver = true;
            
            await GameEnded.InvokeOrDefault(() => new GameEndedNotification
            {
                WinnerPlayerId = gameScores.First().PlayerId,
                PlayerScores = gameScores
            });
        }

        else
        {
            Deck.KnuthShuffle(Seed);
            Deal();
            await StartRoundAsync();
        }
        
        return response;
    }

    public async Task<PutCardsResponse> PutCards(PutCardsRequest request)
    {
        IncrementSeed();
        var playerId = request.PlayerId;
        PutCardsResponse response;
        
        if (!TryGetCurrentPlayer(playerId, out var player))
        {
            response = new PutCardsResponse {Error = "It is not your turn"};
            await RespondAsync(playerId, response);
            return response;
        }
        
        if (!player.HasCards(request.Cards))
        {
            response = new PutCardsResponse { Error = "You don't have those cards" };
            await RespondAsync(playerId, response);
            return response;
        }

        if (!CanPlay(request.Cards, out var error))
        {
            response = new PutCardsResponse { Error = error };
            await RespondAsync(playerId, response);
            return response;
        }

        if (!player.CardsOnHand.TryStealRange(request.Cards, out var cards))
        {
            response = new PutCardsResponse { Error = "You don't have those cards" };
            await RespondAsync(playerId, response);
            return response;
        }

        var drawn = request.DrawCardFrom switch
        {
            DrawCardFrom.DiscardPile => DiscardPile.Pop(),
            DrawCardFrom.StockPile => StockPile.Pop(),
            _ => StockPile.Pop() // ¯\_(ツ)_/¯
        };
        DiscardPile.PushRange(cards);
        player.CardsOnHand.Add(drawn);

        response = new PutCardsResponse
        {
            DrawnCard = drawn
        };
        await RespondAsync(playerId, response);

        await PlayerPutCards.InvokeOrDefault(() => new PlayerPutCardsNotification
        {
            PlayerId = playerId,
            Cards = request.Cards,
            DrewCardFrom = request.DrawCardFrom
        });

        await MoveToNextPlayerAsync();

        return response;
    }

    private async Task MoveToNextPlayerAsync()
    {
        await ShufflePileIfNecessaryAsync();
        CurrentPlayerIndex++;
        if (CurrentPlayerIndex > Players.Count - 1)
        {
            CurrentPlayerIndex = 0;
        }
        
        await ItsYourTurn.InvokeOrDefault(CurrentPlayer.Id, new ItsYourTurnNotification());
    }
    
    private async Task ShufflePileIfNecessaryAsync()
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
        await DiscardPileShuffled.InvokeOrDefault(() => new DiscardPileShuffledNotification
        {
            DiscardPileSize = DiscardPile.Count,
            StockPileSize = StockPile.Count
        });
    }

    private static bool CanPlay(Card[] cards, [MaybeNullWhen(true)] out string error)
    {
        error = default;
        switch (cards.Length)
        {
            case 0:
                error = "You must play at least 1 card";
                return false;
            case 1:
                return true;
            case 2:
                if (cards.AreOfSameRank())
                {
                    return true;
                }
                error = "Cards must be of same rank or straight";
                return false;
            default:
                if (cards.AreOfSameRank() || cards.IsStraight(ValueCaluclation.AceIsOne))
                {
                    return true;
                }

                error = "Cards must be of same rank or straight";
                return false;
        }
    }

    private bool TryGetCurrentPlayer(Guid playerId, [MaybeNullWhen(false)] out YanivPlayer player)
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

    private async Task StartRoundAsync()
    {
        await Task.WhenAll(Players.Select(p => RoundStarted.InvokeOrDefault(p.Id, () => new RoundStartedNotification
        {
            PlayerViewOfGame = new PlayerViewOfGame
            {
                CardsOnHand = p.CardsOnHand.ToList(),
                TopOfPile = TopOfPile.GetValueOrDefault(),
                DeckSize = Deck.Count,
                OtherPlayers = Players.Where(o => o.Id != p.Id).Select(o => new OtherYanivPlayer
                {
                    Id = o.Id,
                    Name = o.Name,
                    NumberOfCards = o.CardsOnHand.Count,
                }).ToArray()
            }
        })));
        
        await ItsYourTurn.InvokeOrDefault(CurrentPlayer.Id, () => new ItsYourTurnNotification());
    }
    
    public override Task StartAsync() => StartRoundAsync();
}