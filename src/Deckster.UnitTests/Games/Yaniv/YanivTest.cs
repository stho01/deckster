using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Yaniv;
using Deckster.Games;
using Deckster.Games.Collections;
using Deckster.Games.Yaniv;
using Deckster.Server.Games;
using Deckster.UnitTests.Fakes;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Yaniv;

public class YanivTest
{
    [Test]
    public async ValueTask PutCards_Straight()
    {
        var cards = Enumerable.Range(5, 7).Select(i => new Card(i, Suit.Diamonds)).ToArray();
        await AssertPutCardsSucceedsAsync(cards);
    }
    
    [Test]
    public async ValueTask PutCards_StraightWithJoker()
    {
        var cards = new[]
        {
            new Card(5, Suit.Diamonds),
            new Card(0, Suit.Clubs),
            new Card(7, Suit.Spades)
        };
        await AssertPutCardsSucceedsAsync(cards);
    }
    
    [Test]
    public async ValueTask PutCards_StraightWithJokerFirst()
    {
        var cards = new[]
        {
            new Card(0, Suit.Diamonds),
            new Card(6, Suit.Clubs),
            new Card(7, Suit.Spades)
        };
        await AssertPutCardsSucceedsAsync(cards);
    }
    
    [Test]
    public async ValueTask PutCards_StraightWithJokerLast()
    {
        var cards = new[]
        {
            new Card(5, Suit.Diamonds),
            new Card(6, Suit.Clubs),
            new Card(0, Suit.Diamonds)
        };
        await AssertPutCardsSucceedsAsync(cards);
    }

    [Test]
    public async ValueTask PutCards_Fails()
    {
        var cards = new[]
        {
            new Card(5, Suit.Diamonds),
            new Card(10, Suit.Clubs),
            new Card(7, Suit.Spades)
        };
        await AssertPutCardsFailAsync(cards, "Cards must be of same rank or straight");
    }
    
    [Test]
    public async ValueTask PutCards_Fails_WhenStraightTooShort()
    {
        var cards = new[]
        {
            new Card(5, Suit.Diamonds),
            new Card(6, Suit.Clubs),
        };
        await AssertPutCardsFailAsync(cards, "Cards must be of same rank or straight");
    }
    
    [Test]
    public async ValueTask PutCards_Fails_WhenNotStraight()
    {
        var cards = new[]
        {
            new Card(5, Suit.Diamonds),
            new Card(6, Suit.Clubs),
            new Card(8, Suit.Clubs),
        };
        await AssertPutCardsFailAsync(cards, "Cards must be of same rank or straight");
    }
    
    [Test]
    public async ValueTask PutCards_MovesToNextPlayer()
    {
        var cards = new[]
        {
            new Card(5, Suit.Diamonds),
            new Card(6, Suit.Clubs),
            new Card(7, Suit.Clubs),
        };
        
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.PushRange(stock.StealRange(cards));
            g.Players[0].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[2].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[3].CardsOnHand.Push(g.StockPile.StealRandom());
        });

        var response = await game.PutCards(new PutCardsRequest {PlayerId = game.Players[0].Id, Cards = cards});
        Asserts.Success(response);
        
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[1]));
    }

    [Test]
    public async ValueTask CallYaniv()
    {
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.Push(stock.Steal(5, Suit.Diamonds));
            g.Players[2].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[3].CardsOnHand.Push(g.StockPile.StealRandom());
        });

        var response = await game.CallYaniv(new CallYanivRequest {PlayerId = game.Players[0].Id});
        Asserts.Success(response);
    }

    [Test]
    public async ValueTask CallYaniv_Fails_WhenPlayerHasTooManyPointsOnHand()
    {
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.Push(stock.Steal(6, Suit.Diamonds));
            g.Players[2].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[3].CardsOnHand.Push(g.StockPile.StealRandom());
        });

        var response = await game.CallYaniv(new CallYanivRequest {PlayerId = game.Players[0].Id});
        Asserts.Fail(response, "You must have 5 points or less on hand");
    }

    [Test]
    public async ValueTask CallYaniv_EndsRound()
    {
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.Push(stock.Steal(5, Suit.Diamonds));
            g.Players[1].CardsOnHand.Push(g.StockPile.Steal(7, Suit.Clubs));
            g.Players[2].CardsOnHand.Push(g.StockPile.Steal(8, Suit.Clubs));
            g.Players[3].CardsOnHand.Push(g.StockPile.Steal(9, Suit.Spades));
        });
        
        var communication = new FakeCommunication(game);

        Asserts.Success(await game.CallYaniv(new CallYanivRequest {PlayerId = game.Players[0].Id}));
        Assert.That(communication.HasBroadcasted<RoundEndedNotification>(n =>
        {
            Assert.That(n.WinnerPlayerId, Is.EqualTo(game.Players[0].Id));
            AssertRoundPoints(n, game.Players[0], 0, 0);
            AssertRoundPoints(n, game.Players[1], 7, 0);
            AssertRoundPoints(n, game.Players[2], 8, 0);
            AssertRoundPoints(n, game.Players[3], 9, 0);

            return true;
        }));
    }
    
    [Test]
    public async ValueTask CallYaniv_StartsNewRound()
    {
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.Push(stock.Steal(5, Suit.Diamonds));
            g.Players[1].CardsOnHand.Push(g.StockPile.Steal(7, Suit.Clubs));
            g.Players[2].CardsOnHand.Push(g.StockPile.Steal(8, Suit.Clubs));
            g.Players[3].CardsOnHand.Push(g.StockPile.Steal(9, Suit.Spades));
        });
        
        var communication = new FakeCommunication(game);

        Asserts.Success(await game.CallYaniv(new CallYanivRequest {PlayerId = game.Players[0].Id}));
        Assert.That(communication.HasNotifiedEachPlayer<RoundStartedNotification>());
    }

    [Test]
    public async ValueTask CallYaniv_EndsGame_WhenSomePlayerHasExceededLimit()
    {
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.Push(stock.Steal(5, Suit.Diamonds));
            g.Players[1].CardsOnHand.Push(g.StockPile.Steal(7, Suit.Clubs));
            g.Players[2].CardsOnHand.Push(g.StockPile.Steal(8, Suit.Clubs));
            g.Players[3].CardsOnHand.Push(g.StockPile.Steal(9, Suit.Spades));
            g.Players[3].Points = 92;
        });
        
        var communication = new FakeCommunication(game);

        Asserts.Success(await game.CallYaniv(new CallYanivRequest {PlayerId = game.Players[0].Id}));
        Assert.That(communication.HasBroadcasted<RoundEndedNotification>());
        Assert.That(communication.HasBroadcasted<GameEndedNotification>());
        Assert.That(game.State == GameState.Finished);
    }
    
    [Test]
    public async ValueTask CallYaniv_EndsGame_WhenSomePlayerHasExactlyLimit()
    {
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.Push(stock.Steal(5, Suit.Diamonds));
            
            g.Players[1].CardsOnHand.Push(g.StockPile.Steal(7, Suit.Clubs));
            g.Players[2].CardsOnHand.Push(g.StockPile.Steal(8, Suit.Clubs));
            g.Players[3].CardsOnHand.Push(g.StockPile.Steal(9, Suit.Spades));
            g.Players[3].Points = 91;
        });
        
        var communication = new FakeCommunication(game);

        Asserts.Success(await game.CallYaniv(new CallYanivRequest {PlayerId = game.Players[0].Id}));
        Assert.That(communication.HasBroadcasted<RoundEndedNotification>(n =>
        {
            var roundWinner = game.Players[0];
            Assert.That(n.WinnerPlayerId, Is.EqualTo(roundWinner.Id));
            
            AssertRoundPoints(n, roundWinner, 0, 0);
            return true;
        }));
        Assert.That(communication.HasBroadcasted<GameEndedNotification>(n =>
        {
            var finalWinner = game.Players[3];
            AssertFinalPoints(n, finalWinner, 100, 0, 0);
            return true;
        }));
        Assert.That(game.State == GameState.Finished);
    }
    
    [Test]
    public async ValueTask CallYaniv_AddsPenaltyIfOtherPlayerHasLessPointsThanCaller()
    {
        var game = SetUpGame(g =>
        {
            var stock = g.StockPile;
            g.Players[0].CardsOnHand.Push(stock.Steal(5, Suit.Diamonds));
            g.Players[1].CardsOnHand.Push(g.StockPile.Steal(5, Suit.Clubs));
            g.Players[2].CardsOnHand.Push(g.StockPile.Steal(8, Suit.Clubs));
            g.Players[3].CardsOnHand.Push(g.StockPile.Steal(9, Suit.Spades));
        });
        
        var communication = new FakeCommunication(game);

        Asserts.Success(await game.CallYaniv(new CallYanivRequest {PlayerId = game.Players[0].Id}));
        Assert.That(communication.HasBroadcasted<RoundEndedNotification>(n =>
        {
            Assert.That(n.WinnerPlayerId, Is.EqualTo(game.Players[1].Id));
            AssertRoundPoints(n, game.Players[0], 5, YanivGame.Penalty);
            AssertRoundPoints(n, game.Players[1], 5, 0);
            AssertRoundPoints(n, game.Players[2], 8, 0);
            AssertRoundPoints(n, game.Players[3], 9, 0);

            return true;
        }));
    }

    private static void AssertFinalPoints(GameEndedNotification n, YanivPlayer player, int points, int penalty, int finalPoints)
    {
        Assert.That(player.Points, Is.EqualTo(points));
        Assert.That(player.Penalty, Is.EqualTo(penalty));

        var score = n.PlayerScores.Single(s => s.PlayerId == player.Id);
        Assert.That(score.Points, Is.EqualTo(points));
        Assert.That(score.Penalty, Is.EqualTo(penalty));
        Assert.That(score.FinalPoints, Is.EqualTo(finalPoints));
    }
    
    private static void AssertRoundPoints(RoundEndedNotification n, YanivPlayer player, int points, int penalty)
    {
        Assert.That(player.Points, Is.EqualTo(points));
        Assert.That(player.Penalty, Is.EqualTo(penalty));

        var score = n.PlayerScores.Single(s => s.PlayerId == player.Id);
        Assert.That(score.Points, Is.EqualTo(points));
        Assert.That(score.Penalty, Is.EqualTo(penalty));
    }

    private static async ValueTask AssertPutCardsSucceedsAsync(Card[] cards)
    {
        var game = SetUpGame(g =>
        {
            var theCards = g.StockPile.StealRange(cards);
            g.Players[0].CardsOnHand.PushRange(theCards);
            g.Players[1].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[2].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[3].CardsOnHand.Push(g.StockPile.StealRandom());
        });

        var response = await game.PutCards(new PutCardsRequest
        {
            PlayerId = game.Players[0].Id,
            Cards = cards,
            DrawCardFrom = DrawCardFrom.StockPile
        });
        
        Asserts.Success(response);
    }
    
    private static async ValueTask AssertPutCardsFailAsync(Card[] cards, string error)
    {
        var game = SetUpGame(g =>
        {
            var theCards = g.StockPile.StealRange(cards);
            g.Players[0].CardsOnHand.PushRange(theCards);
            g.Players[1].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[2].CardsOnHand.Push(g.StockPile.StealRandom());
            g.Players[3].CardsOnHand.Push(g.StockPile.StealRandom());
        });

        var response = await game.PutCards(new PutCardsRequest
        {
            PlayerId = game.Players[0].Id,
            Cards = cards,
            DrawCardFrom = DrawCardFrom.StockPile
        });
        
        Asserts.Fail(response, error);
    }
    
    private static YanivGame SetUpGame(Action<YanivGame> configure)
    {
        var game = YanivGame.Create(new YanivGameCreatedEvent
        {
            Id = Some.Id,
            Players = Some.FourPlayers(),
            Deck = Decks.Standard().WithJokers(2)
        });
        game.StockPile.PushRange(game.Deck);
        
        configure(game);
        
        return game;
    }
}