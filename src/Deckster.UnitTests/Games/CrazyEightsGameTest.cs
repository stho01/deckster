using System.Text.Json;
using System.Text.Json.Serialization;
using Deckster.Client.Common;
using Deckster.Client.Games.Common;
using Deckster.Server.Games.CrazyEights;
using NUnit.Framework;

namespace Deckster.UnitTests.Games;

[TestFixture]
public class CrazyEightsGameTest
{
    [Test]
    public void Print()
    {
        var game = CreateGame();
        Console.WriteLine(game.Deck.Cards.Count);
        Console.WriteLine(JsonSerializer.Serialize(game.Players[0], new JsonSerializerOptions{WriteIndented = true, Converters = { new JsonStringEnumConverter() }}));
        Console.WriteLine(game.TopOfPile);
    }

    [Test]
    [TestCase(12, Suit.Hearts)]
    [TestCase(8, Suit.Spades)]
    [TestCase(4, Suit.Spades)]
    public void PutCard_Succeeds(int rank, Suit suit)
    {
        var game = CreateGame();
        var player = game.Players[0];
        var card = new Card(rank, suit);
        var result = game.PutCard(player.Id, card);
        AssertSuccess(result);
    }
    
    [Test]
    public void PutCard_Fails_WhenNotYourTurn()
    {
        var game = CreateGame();
        var player = game.Players[1];
        var result = game.PutCard(player.Id, player.Cards[0]);
        
        AssertFail(result, "It is not your turn");
    }

    [Test]
    [TestCase(1, Suit.Clubs, "You don't have 'A♧'")]
    [TestCase(12, Suit.Clubs, "Cannot put 'Q♧' on '4♥'")]
    public void PutCard_Fails(int rank, Suit suit, string errorMessage)
    {
        var game = CreateGame();
        var player = game.Players[0];
        var card = new Card(rank, suit);
        
        var result = game.PutCard(player.Id, card);
        AssertFail(result, errorMessage);
    }

    [Test]
    public void DrawCard_Fails_AfterThreeAttempts()
    {
        var game = CreateGame();
        var player = game.Players[0];

        for (var ii = 0; ii < 3; ii++)
        {
            game.DrawCard(player.Id);
        }
        
        var result = game.DrawCard(player.Id);
        AssertFail(result, "You can only draw 3 cards");
    }

    [Test]
    public void Pass_SucceedsAlways()
    {
        var game = CreateGame();
        var player = game.Players[0];
        var result = game.Pass(player.Id);
        AssertSuccess(result);
    }

    [Test]
    [TestCase(Suit.Clubs)]
    [TestCase(Suit.Diamonds)]
    [TestCase(Suit.Spades)]
    [TestCase(Suit.Hearts)]
    public void PutEight_ChangesSuit(Suit newSuit)
    {
        var game = CreateGame();
        var player = game.Players[0];
        var eight = new Card(8, Suit.Spades);
        AssertSuccess(game.PutEight(player.Id, eight, newSuit));
        Assert.That(game.CurrentSuit, Is.EqualTo(newSuit));

        var nextPlayer = game.CurrentPlayer;
        var cardWithNewSuit = nextPlayer.Cards.First(c => c.Suit == newSuit && c.Rank != 8);
        
        AssertSuccess(game.PutCard(nextPlayer.Id, cardWithNewSuit));
    }

    [Test]
    public void PutEight_Fails_WhenNotEight()
    {
        var game = CreateGame();
        var player = game.Players[0];
        var notEight = player.Cards[0];
        var result = game.PutEight(player.Id, notEight, Suit.Clubs); 
        AssertFail(result, "Card rank must be '8'");
    }

    [Test]
    public void Draw_Fails_WhenNoMoreCards()
    {
        var game = CreateGame(12);
        var player = game.Players[0];
        for (var ii = 0; ii < 3; ii++)
        {
            AssertSuccess(game.DrawCard(player.Id));
        }

        AssertSuccess(game.Pass(player.Id));
        
        var result = game.DrawCard(game.CurrentPlayer.Id);
        Console.WriteLine(Pretty(result));
        AssertFail(result, "No more cards");
    }

    private static string Pretty(object thing)
    {
        return JsonSerializer.Serialize(thing, new JsonSerializerOptions {WriteIndented = true, Converters = {new JsonStringEnumConverter()}});
    }

    private static void AssertSuccess(CommandResult result)
    {
        switch (result)
        {
            case SuccessResult:
                break;
            case FailureResult r:
                Assert.Fail($"Expeced success, but got '{r.Message}'");
                break;
        }
    }

    private static void AssertFail(CommandResult result, string message)
    {
        switch (result)
        {
            case FailureResult r:
                Assert.That(r.Message, Is.EqualTo(message));
                break;
            default:
                Assert.Fail($"Expected failure, but got {result.GetType().Name}");
                break;
        }
    }

    private static CrazyEightsGame CreateGame(int cardsPerPlayer = 10)
    {
        var players = new[]
        {
            new CrazyEightsPlayer
            {
                Id = Some.Id,
                Name = Some.PlayerName
            },
            new CrazyEightsPlayer
            {
                Id = Some.OtherId,
                Name = Some.OtherPlayerName
            },
            new CrazyEightsPlayer
            {
                Id = Some.YetAnotherId,
                Name = Some.YetAnotherPlayerName
            },
            new CrazyEightsPlayer
            {
                Id = Some.TotallyDifferentId,
                Name = Some.TotallyDifferentPlayerName
            }
        };
        var game = new CrazyEightsGame(TestDeck, players, cardsPerPlayer);
        return game;
    }

    private static Deck TestDeck => new(GetCards());

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