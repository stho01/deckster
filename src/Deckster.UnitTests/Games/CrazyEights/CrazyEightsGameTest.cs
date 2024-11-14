using Deckster.Client.Games.CrazyEights;
using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.CrazyEights;
using Deckster.Core.Serialization;
using Deckster.Games.Collections;
using Deckster.Games.CrazyEights;
using Deckster.Server.Games.CrazyEights;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.CrazyEights;

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
            g.CurrentPlayer.Cards.Add(cards.Steal(new Card(10, Suit.Hearts)));
            g.DiscardPile.Push(cards.Steal(new Card(9, Suit.Hearts)));
        });
        
        var card = new Card(10, Suit.Hearts);
        var result = await game.PutCard(new PutCardRequest{ PlayerId = game.CurrentPlayer.Id, Card = card });
        Asserts.Success(result);
    }
    
    [Test]
    public async Task PutCard_Fails_WhenNotYourTurn()
    {
        var game = CreateGame();
        var player = game.Players[1];
        var result = await game.PutCard(new PutCardRequest{ PlayerId = player.Id, Card = player.Cards[0] });

        Asserts.Fail(result, "It is not your turn");
    }

    [Test]
    [TestCase(1, Suit.Clubs, "You don't have 'A♧'")]
    [TestCase(12, Suit.Clubs, "Cannot put 'Q♧' on '4♤'")]
    public async Task PutCard_Fails(int rank, Suit suit, string errorMessage)
    {
        var game = SetUpGame(g =>
        {
            var cards = g.Deck;
            g.Players[0].Cards.Add(cards.Steal(11, Suit.Hearts));
            g.Players[0].Cards.Add(cards.Steal(12, Suit.Clubs));
            
            g.Players[1].Cards.Add(cards.Steal(8, Suit.Diamonds));
            
            g.DiscardPile.Push(cards.Steal(4, Suit.Spades));
        });
        var player = game.Players[0];
        var card = new Card(rank, suit);
        
        var result = await game.PutCard(new PutCardRequest{ PlayerId = player.Id, Card = card });
        Asserts.Fail(result, errorMessage);
    }

    [Test]
    public async Task DrawCard_Fails_AfterThreeAttempts()
    {
        var game = CreateGame();
        var player = game.Players[0];

        for (var ii = 0; ii < 3; ii++)
        {
            await game.DrawCard(new DrawCardRequest{ PlayerId = player.Id });
        }
        
        var result = await game.DrawCard(new DrawCardRequest{ PlayerId = player.Id });
        Asserts.Fail(result, "You can only draw 3 cards");
    }

    [Test]
    public async Task Pass_SucceedsAlways()
    {
        var game = CreateGame();
        var player = game.Players[0];
        var result = await game.Pass(new PassRequest{ PlayerId = player.Id });
        Asserts.Success(result);
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
            g.Players[0].Cards.Add(cards.Steal(8, Suit.Clubs));
            g.Players[0].Cards.Add(cards.Steal(8, Suit.Diamonds));
            g.Players[0].Cards.Add(cards.Steal(8, Suit.Spades));
            g.Players[0].Cards.Add(cards.Steal(8, Suit.Hearts));
            
            g.Players[1].Cards.Add(cards.Steal(9, Suit.Clubs));
            g.Players[1].Cards.Add(cards.Steal(9, Suit.Diamonds));
            g.Players[1].Cards.Add(cards.Steal(9, Suit.Spades));
            g.Players[1].Cards.Add(cards.Steal(9, Suit.Hearts));
            
            g.DiscardPile.Push(cards.Steal(10, Suit.Clubs));
        });
        var player = game.CurrentPlayer;
        var eight = new Card(8, Suit.Spades);
        Asserts.Success(await game.PutEight(new PutEightRequest{ PlayerId = player.Id, Card = eight, NewSuit = newSuit }));
        Assert.That(game.CurrentSuit, Is.EqualTo(newSuit));

        var nextPlayer = game.CurrentPlayer;
        var cardWithNewSuit = nextPlayer.Cards.First(c => c.Suit == newSuit && c.Rank != 8);

        Asserts.Success(await game.PutCard(new PutCardRequest{ PlayerId = nextPlayer.Id, Card = cardWithNewSuit }));
    }

    [Test]
    public async Task PutEight_Fails_WhenNotEight()
    {
        var game = CreateGame();
        var player = game.Players[0];
        var notEight = player.Cards[0];
        var result = await game.PutEight(new PutEightRequest{ PlayerId = player.Id, Card = notEight, NewSuit = Suit.Clubs });
        Asserts.Fail(result, "Card rank must be '8'");
    }

    [Test]
    public async Task Draw_Fails_WhenNoMoreCards()
    {
        var game = CreateGame();
        game.StockPile.Clear();
        
        var result = await game.DrawCard(new DrawCardRequest{ PlayerId = game.CurrentPlayer.Id });
        Console.WriteLine(result.Pretty());
        Asserts.Fail(result, "Stock pile is empty");
    }

    private static CrazyEightsGame SetUpGame(Action<CrazyEightsGame> configure)
    {
        var game = CrazyEightsGame.Instantiate(new CrazyEightsGameCreatedEvent
        {
            Id = Some.Id,
            Players = Some.FourPlayers(),
            Deck = TestDeck
        });

        configure(game);
        
        return game;
    }

    private static CrazyEightsGame CreateGame()
    {
        return CrazyEightsGame.Instantiate(new CrazyEightsGameCreatedEvent
        {
            Players = Some.FourPlayers(),
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
}