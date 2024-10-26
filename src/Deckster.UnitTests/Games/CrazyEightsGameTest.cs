using System.Text.Json;
using System.Text.Json.Serialization;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;
using Deckster.Server.Collections;
using Deckster.Server.Games.CrazyEights.Core;
using NUnit.Framework;

namespace Deckster.UnitTests.Games;

[TestFixture]
public class CrazyEightsGameTest
{
    [Test]
    public void Print()
    {
        var game = CreateGame();
        Console.WriteLine(game.Deck.Count);
        Console.WriteLine(game.Players[0].Pretty());
        Console.WriteLine(game.TopOfPile);
    }

    [Test]
    public async Task PutCard_Succeeds()
    {
        var game = SetUpGame(g =>
        {
            var cards = g.Deck;
            g.CurrentPlayer.Cards.Add(cards.Get(new Card(10, Suit.Hearts)));
            g.DiscardPile.Push(cards.Get(new Card(9, Suit.Hearts)));
        });
        
        var card = new Card(10, Suit.Hearts);
        var result = await game.PutCard(game.CurrentPlayer.Id, card);
        AssertSuccess(result);
    }
    
    [Test]
    public async Task PutCard_Fails_WhenNotYourTurn()
    {
        var game = CreateGame();
        var player = game.Players[1];
        var result = await game.PutCard(player.Id, player.Cards[0]);
        
        AssertFail(result, "It is not your turn");
    }

    [Test]
    [TestCase(1, Suit.Clubs, "You don't have 'A♧'")]
    [TestCase(12, Suit.Clubs, "Cannot put 'Q♧' on '4♤'")]
    public async Task PutCard_Fails(int rank, Suit suit, string errorMessage)
    {
        var game = SetUpGame(g =>
        {
            var cards = g.Deck;
            g.Players[0].Cards.Add(cards.Get(11, Suit.Hearts));
            g.Players[0].Cards.Add(cards.Get(12, Suit.Clubs));
            
            g.Players[1].Cards.Add(cards.Get(8, Suit.Diamonds));
            
            g.DiscardPile.Push(cards.Get(4, Suit.Spades));
        });
        var player = game.Players[0];
        var card = new Card(rank, suit);
        
        var result = await game.PutCard(player.Id, card);
        AssertFail(result, errorMessage);
    }

    [Test]
    public async Task DrawCard_Fails_AfterThreeAttempts()
    {
        var game = CreateGame();
        var player = game.Players[0];

        for (var ii = 0; ii < 3; ii++)
        {
            await game.DrawCard(player.Id);
        }
        
        var result = await game.DrawCard(player.Id);
        AssertFail(result, "You can only draw 3 cards");
    }

    [Test]
    public async Task Pass_SucceedsAlways()
    {
        var game = CreateGame();
        var player = game.Players[0];
        var result = await game.Pass(player.Id);
        AssertSuccess(result);
    }

    [Test]
    [TestCase(Suit.Clubs)]
    [TestCase(Suit.Diamonds)]
    [TestCase(Suit.Spades)]
    [TestCase(Suit.Hearts)]
    public async Task PutEight_ChangesSuit(Suit newSuit)
    {
        var game = SetUpGame(g =>
        {
            var cards = TestDeck;
            g.Players[0].Cards.Add(cards.Get(8, Suit.Clubs));
            g.Players[0].Cards.Add(cards.Get(8, Suit.Diamonds));
            g.Players[0].Cards.Add(cards.Get(8, Suit.Spades));
            g.Players[0].Cards.Add(cards.Get(8, Suit.Hearts));
            
            g.Players[1].Cards.Add(cards.Get(9, Suit.Clubs));
            g.Players[1].Cards.Add(cards.Get(9, Suit.Diamonds));
            g.Players[1].Cards.Add(cards.Get(9, Suit.Spades));
            g.Players[1].Cards.Add(cards.Get(9, Suit.Hearts));
            
            g.DiscardPile.Push(cards.Get(10, Suit.Clubs));
        });
        var player = game.CurrentPlayer;
        var eight = new Card(8, Suit.Spades);
        AssertSuccess(await game.PutEight(player.Id, eight, newSuit));
        Assert.That(game.CurrentSuit, Is.EqualTo(newSuit));

        var nextPlayer = game.CurrentPlayer;
        var cardWithNewSuit = nextPlayer.Cards.First(c => c.Suit == newSuit && c.Rank != 8);
        
        AssertSuccess(await game.PutCard(nextPlayer.Id, cardWithNewSuit));
    }

    [Test]
    public async Task PutEight_Fails_WhenNotEight()
    {
        var game = CreateGame();
        var player = game.Players[0];
        var notEight = player.Cards[0];
        var result = await game.PutEight(player.Id, notEight, Suit.Clubs); 
        AssertFail(result, "Card rank must be '8'");
    }

    [Test]
    public async Task Draw_Fails_WhenNoMoreCards()
    {
        var game = CreateGame();
        game.StockPile.Clear();
        
        var result = await game.DrawCard(game.CurrentPlayer.Id);
        Console.WriteLine(Pretty(result));
        AssertFail(result, "Stock pile is empty");
    }

    private static void AssertSuccess(DecksterResponse result)
    {
        switch (result)
        {
            case PassOkResponse:
                break;
            case FailureResponse r:
                Assert.Fail($"Expeced success, but got '{r.Message}'");
                break;
        }
    }

    private static void AssertFail(DecksterResponse result, string message)
    {
        switch (result)
        {
            case FailureResponse r:
                Assert.That(r.Message, Is.EqualTo(message));
                break;
            default:
                Assert.Fail($"Expected failure, but got {result.GetType().Name}");
                break;
        }
    }
    
    private static CrazyEightsGame SetUpGame(Action<CrazyEightsGame> configure)
    {
        var players = new List<PlayerData>
        {
            new()
            {
                Id = Some.Id,
                Name = Some.PlayerName
            },
            new()
            {
                Id = Some.OtherId,
                Name = Some.OtherPlayerName
            },
            new()
            {
                Id = Some.YetAnotherId,
                Name = Some.YetAnotherPlayerName
            },
            new()
            {
                Id = Some.TotallyDifferentId,
                Name = Some.TotallyDifferentPlayerName
            }
        };

        var game = CrazyEightsGame.Create(new CrazyEightsGameCreatedEvent
        {
            Id = Some.Id,
            Players = players,
            Deck = TestDeck
        });

        configure(game);
        
        return game;
    }

    private static CrazyEightsGame CreateGame()
    {
        return CrazyEightsGame.Create(new CrazyEightsGameCreatedEvent
        {
            Players =
            [
                new()
                {
                    Id = Some.Id,
                    Name = Some.PlayerName
                },

                new()
                {
                    Id = Some.OtherId,
                    Name = Some.OtherPlayerName
                },

                new()
                {
                    Id = Some.YetAnotherId,
                    Name = Some.YetAnotherPlayerName
                },

                new()
                {
                    Id = Some.TotallyDifferentId,
                    Name = Some.TotallyDifferentPlayerName
                }
            ],
            Deck = TestDeck,
            InitialSeed = Some.Seed 
        });
    }

    private static List<Card> TestDeck => GetCards().ToList();

    // Make sure all players have all suits
    private static IEnumerable<Card> GetCards()
    {
        var ranks = new Dictionary<Suit, int>
        {
            [Suit.Clubs] = 0,
            [Suit.Diamonds] = 0,
            [Suit.Spades] = 0,
            [Suit.Hearts] = 0
        };
        
        while (ranks.Values.Any(v => v < 13))
        {
            foreach (var suit in Enum.GetValues<Suit>())
            {
                for (var ii = 0; ii < 4; ii++)
                {
                    var rank = ranks[suit] + 1;
                    if (rank > 13)
                    {
                        continue;
                    }
                    ranks[suit] = rank;

                    yield return new Card(rank, suit);    
                }
            }
        }
    }
    
    private static string Pretty(object thing)
    {
        return JsonSerializer.Serialize(thing, new JsonSerializerOptions {WriteIndented = true, Converters = {new JsonStringEnumConverter()}});
    }
}

public static class ListExtensions
{
    public static Card Get(this List<Card> cards, int rank, Suit suit) => cards.Get(new Card(rank, suit));
    
    public static Card Get(this List<Card> cards, Card card)
    {
        if (!cards.Remove(card))
        {
            throw new InvalidOperationException($"List does not contain {card}");
        }

        return card;
    }
}