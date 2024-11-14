using Deckster.Core.Collections;
using Deckster.Core.Extensions;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Gabong;
using Deckster.Games.Gabong;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Gabong;

public class GabongGameTest
{
    [Test]
    public async ValueTask PutCard_Succeeds()
    {
        var game = await SetUpGame(g =>
        {
            g.CurrentPlayer.Cards.Add(8.OfClubs());
            g.DiscardPile.Push(7.OfDiamonds());
        });
        
        var card = 8.OfClubs();
        var result = await game.PutCard(new PutCardRequest(){ PlayerId = game.CurrentPlayer.Id, Card = card, NewSuit = Suit.Hearts});
        Asserts.Success(result);
    }

    [Test]
    public async ValueTask PutCard_Fails_WhenNotYourTurn()
    {
        var game = await SetUpGame(g =>
        {
            g.Players[2].Cards.Add(8.OfClubs());
            g.DiscardPile.Push(7.OfDiamonds());
        });
        
        var player = game.Players[2];
        var result = await game.PutCard(new PutCardRequest{ PlayerId = player.Id, Card = 8.OfClubs() });

        Asserts.Fail(result, "NO! It is not your turn");
    }

    [Test]
    public async ValueTask PutCard_Allowed_WhenNotYourTurn_IfIdenticalCard()
    {
        var game = await SetUpGame(g =>
        {
            g.Players[2].Cards.Add(7.OfDiamonds());
            g.DiscardPile.Push(7.OfDiamonds());
        });
        
        var player = game.Players[2];
        var result = await game.PutCard(new PutCardRequest{ PlayerId = player.Id, Card = 7.OfDiamonds() });

        Asserts.Success(result);
    }

 
    [Test]
    public async ValueTask TurnOrderIsRespected()
    {
        var game = await SetUpGame(TweakToControlledGame);
        var finalResult = await PlayUntilError(game,[
            (0, 5.OfClubs()), 
            (1, 6.OfClubs()), 
            (2, 7.OfClubs()), 
            (3, 8.OfClubs())]);
        Asserts.Success(finalResult);
    }

    [Test]
    public async ValueTask MakeSureThatIdenticalCardsAreAllowedAlways()
    {
        var game = await SetUpGame(TweakToControlledGame);
        var finalResult = await PlayUntilError(game,[
            (0, 4.OfClubs()), 
            (3, 4.OfClubs()), 
            (2, 4.OfClubs()), 
            (3, 12.OfClubs())]);
        Asserts.Success(finalResult);
    }

    [Test]
    public async ValueTask MakeSureThat3SkipsPlay()
    {
        var game = await SetUpGame(TweakToControlledGame);
        var finalResult = await PlayUntilError(game,[
            (0, 4.OfClubs()), 
            (1, 3.OfClubs()), 
            (3, 4.OfClubs()), 
            (0, 5.OfClubs())
        ]);
        Asserts.Success(finalResult);
    }

    [Test]
    public async ValueTask MakeSureThat13ReversesPlay()
    {
        var game = await SetUpGame(TweakToControlledGame);
        var finalResult = await PlayUntilError(game,[
            (0, 4.OfClubs()), 
            (1, 13.OfClubs()), 
            (0, 1.OfClubs()), 
            (3, 4.OfClubs())
        ]);
        Asserts.Success(finalResult);
    }

    [Test]
    public async ValueTask MakeSureThat2DrawsTwoCards()
    {
        var game = await SetUpGame(TweakToControlledGame);
        var finalResult = await PlayUntilError(game,[
            (0, 4.OfClubs()), 
            (1, 2.OfClubs()), 
            (2, 3.OfClubs()) 
        ]);
        Asserts.Fail(finalResult, "NO! You have to draw 2 more cards");
    }

    [Test]
    public async ValueTask MakeSureThatYouDonthaveToDraw2IfNotYourTurn()
    {
        var game = await SetUpGame(TweakToControlledGame);
        var finalResult = await PlayUntilError(game,[
            (0, 4.OfClubs()), 
            (1, 2.OfClubs()), 
            (3, 3.OfClubs()) 
        ]);
        Asserts.Fail(finalResult, "NO! It is not your turn");
    }
    
    [Test]
    public async ValueTask MakeSureThatRoundEndsWhenOnePlayerIsDoneWithHand()
    {
        bool ended = false;
        var game = await SetUpGame(TweakToControlledGame);
        game.Players[1].Cards.Clear();
        game.Players[1].Cards.Add(4.OfClubs());

        var finalResult = await PlayUntilError(game,[
            (0, 4.OfClubs()), 
            (1, 4.OfClubs())
        ]);
        Assert.That(game.Players[1].Score == 0);
        Assert.That(game.Players[0].Score != 0);
        
        Assert.That(game.Players[1].Cards.Count == 7);
        Assert.That(game.CurrentPlayer.Id == game.Players[0].Id);
    }

    [Test]
    public async ValueTask TestGabongCalculations()
    {
        await AssertGabongInGabongReadyGame(0, 7, true);
        await AssertGabongInGabongReadyGame(0, 6, false);
        await AssertGabongInGabongReadyGame(1, 12, true);
        await AssertGabongInGabongReadyGame(1, 13, false);
        // await AssertGabongInGabongReadyGame(3, 1, true);
    }

    private async ValueTask AssertGabongInGabongReadyGame(int playerIndex, int topCard, bool shouldWork)
    {
        var game = await SetUpGame(TweakToGabongReadyGame);
        game.DiscardPile.Push(topCard.OfClubs());
        var player = game.Players[playerIndex];
        var handSizeBefore = player.Cards.Count;
        var result = await game.PlayGabong(new PlayGabongRequest(){ PlayerId = player.Id });
        if (shouldWork)
        {
            Assert.That(game.Players[playerIndex].Score == -5);
            Assert.That(game.Players[(playerIndex+1)%game.Players.Count].Score > 0);
        }
        else
        {
            Assert.That(player.Cards.Count == (handSizeBefore+2));
            Asserts.Fail(result, "NO! You don't have Gabong");
        }
    }


    private async ValueTask<PlayerViewOfGame?> PlayUntilError(GabongGame game, List<(int playerIndex, Card card)> plays)
    {
        PlayerViewOfGame result = null;
        foreach (var (playerIndex, card) in plays)
        {
            result = await game.PutCard(new PutCardRequest
                { PlayerId = game.Players[playerIndex].Id, Card = card });
            if (result.Error.Exists())
            {
                return result;
            }
        }
        return result;
    }


    private void TweakToControlledGame(GabongGame g)
    {
        AssignCardsToPlayers(g, [
            /*0*/   [1.OfClubs(), 3.OfClubs(), 3.OfHearts(), 4.OfClubs(), 2.OfClubs(), 5.OfClubs(), 9.OfClubs(), 1.OfClubs(), 13.OfClubs()],
            /*1*/   [2.OfClubs(), 3.OfClubs(), 3.OfHearts(), 4.OfClubs(), 2.OfClubs(), 6.OfClubs(), 10.OfClubs(), 2.OfClubs(), 13.OfClubs()],
            /*2*/   [3.OfClubs(), 3.OfClubs(), 3.OfHearts(), 4.OfClubs(), 2.OfClubs(), 7.OfClubs(), 11.OfClubs(), 3.OfClubs(), 13.OfClubs()],
            /*3*/   [4.OfClubs(), 3.OfClubs(), 3.OfHearts(), 4.OfClubs(), 2.OfClubs(), 8.OfClubs(), 12.OfClubs(), 4.OfClubs(), 13.OfClubs()]
        ]);
        g.DiscardPile.Push(5.OfClubs());
        g.LastPlayMadeByPlayerIndex = 0;
        g.GabongMasterId = g.Players[0].Id;
    }
    
    private void TweakToGabongReadyGame(GabongGame g)
    {
        AssignCardsToPlayers(g, [
            /*7*/   [4.OfClubs(), 3.OfClubs(), 4.OfHearts(), 3.OfClubs()],
            /*12*/   [4.OfClubs(), 4.OfClubs(), 4.OfHearts(), 12.OfClubs(), 12.OfClubs()],
            /*13*/   [1.OfClubs(), 12.OfClubs(), 2.OfHearts(), 11.OfClubs(), 3.OfClubs(), 10.OfClubs(), 4.OfClubs(), 9.OfClubs(), 5.OfClubs(), 8.OfClubs(), 7.OfClubs(), 8.OfClubs() ],
            /*14*/   [1.OfClubs(), 13.OfClubs(), 1.OfHearts()]
        ]);
        g.DiscardPile.Push(12.OfClubs());
        g.LastPlayMadeByPlayerIndex = 0;
        g.GabongMasterId = g.Players[0].Id;
    }

    private void AssignCardsToPlayers(GabongGame gabongGame, List<List<Card>> cardSetup)
    {
        if (cardSetup.Count != gabongGame.Players.Count)
        {
            throw new ArgumentException("Specify cards for each player");
        }

        for(int i = 0; i<cardSetup.Count; i++)
        {
            gabongGame.Players[i].Cards.Clear();
            gabongGame.Players[i].Cards.AddRange(cardSetup[i]);
        }
    }


    //
    // [Test]
    // [TestCase(1, Suit.Clubs, "You don't have 'ace of spaced'")]
    // [TestCase(7, Suit.Diamonds, "Cannot put 'Seven Blue' on 'One Green'")]
    // public async Task PutCard_Fails(UnoValue value, UnoColor color, string errorMessage)
    // {
    //     var game = SetUpGame(g =>
    //     {
    //         var cards = g.Deck;
    //         g.Players[0].Cards.Add(cards.Get(UnoValue.Eight, UnoColor.Blue));
    //         g.Players[0].Cards.Add(cards.Get(UnoValue.Seven, UnoColor.Blue));
    //         
    //         g.Players[1].Cards.Add(cards.Get(UnoValue.Eight, UnoColor.Red));
    //         
    //         g.DiscardPile.Push(cards.Get(UnoValue.One, UnoColor.Green));
    //     });
    //     var player = game.Players[0];
    //     var card = new UnoCard(value, color);
    //     
    //     var result = await game.PutCard(new PutCardRequest{ PlayerId = player.Id, Card = card });
    //     Asserts.Fail(result, errorMessage);
    // }
    //
    // [Test]
    // public async Task DrawCard_Fails_AfterThreeAttempts()
    // {
    //     var game = CreateGame();
    //     var player = game.Players[0];
    //
    //     var request = new DrawCardRequest{ PlayerId = player.Id };
    //     for (var ii = 0; ii < 2; ii++)
    //     {
    //         await game.DrawCard(request);
    //     }
    //     
    //     var result = await game.DrawCard(request);
    //     Asserts.Fail(result, "You can only draw 1 card, then pass if you can't play");
    // }
    //
    // [Test]
    // public async Task Pass_Fails_WhenCardIsNotDrawn()
    // {
    //     var game = CreateGame();
    //     var player = game.Players[0];
    //     var result = await game.Pass(new PassRequest{ PlayerId = player.Id });
    //     Asserts.Fail(result, "You have to draw a card first");
    // }
    //
    // [Test]
    // public async Task Pass_SucceedsAlways()
    // {
    //     var game = CreateGame();
    //     var player = game.Players[0];
    //     Asserts.Success(await game.DrawCard(new DrawCardRequest{ PlayerId = player.Id }));
    //     var result = await game.Pass(new PassRequest{ PlayerId = player.Id });
    //     Asserts.Success(result);
    // }
    //
    // [Test]
    // [TestCase(UnoColor.Blue)]
    // [TestCase(UnoColor.Red)]
    // [TestCase(UnoColor.Green)]
    // [TestCase(UnoColor.Yellow)]
    // public async Task PutWild_ChangesColor(UnoColor newColor)
    // {
    //     var game = SetUpGame(g =>
    //     {
    //         var cards = TestDeck;
    //         g.Players[0].Cards.Add(cards.Get(UnoValue.Wild, UnoColor.Wild));
    //         g.Players[0].Cards.Add(cards.Get(UnoValue.Eight, UnoColor.Red));
    //         
    //         g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Blue));
    //         g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Red));
    //         g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Green));
    //         g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Yellow));
    //         
    //         g.DiscardPile.Push(cards.Get(UnoValue.Seven, UnoColor.Blue));
    //     });
    //     var player = game.CurrentPlayer;
    //     var eight = new UnoCard(UnoValue.Wild, UnoColor.Wild);
    //     Asserts.Success(await game.PutWild(new PutWildRequest{ PlayerId = player.Id, Card = eight, NewColor = newColor }));
    //     Assert.That(game.CurrentColor, Is.EqualTo(newColor));
    //
    //     var nextPlayer = game.CurrentPlayer;
    //     var cardWithNewSuit = nextPlayer.Cards.First(c => c.Color == newColor);
    //
    //     Asserts.Success(await game.PutCard(new PutCardRequest{ PlayerId = nextPlayer.Id, Card = cardWithNewSuit }));
    // }
    //
    // [Test]
    // public async Task PutWild_Fails_WhenNotWild()
    // {
    //     var game = SetUpGame(g =>
    //     {
    //         var deck = g.Deck;
    //         g.Players[0].Cards.Add(deck.Get(UnoValue.Eight, UnoColor.Blue));
    //     });
    //     var player = game.Players[0];
    //     var notWild = new UnoCard(UnoValue.Eight, UnoColor.Blue);
    //     var result = await game.PutWild(new PutWildRequest{ PlayerId = player.Id, Card = notWild, NewColor = Some.UnoColor });
    //     Asserts.Fail(result, "Eight Blue is not a wildcard");
    // }
    //
    // [Test]
    // public async Task Draw_Fails_WhenNoMoreCards()
    // {
    //     var game = CreateGame();
    //     game.StockPile.Clear();
    //     
    //     var result = await game.DrawCard(new DrawCardRequest{ PlayerId = game.CurrentPlayer.Id });
    //     Console.WriteLine(result.Pretty());
    //     Asserts.Fail(result, "No more cards");
    // }
    
    private static async Task<GabongGame> SetUpGame(Action<GabongGame> tweakGameAfterStarting)
    {
        var players = Some.FourPlayers();

        var game = GabongGame.Instantiate(new GabongGameCreatedEvent
        {
            Id = Some.Id,
            Players = players,
            Deck = TestDeck
        });
        await game.StartAsync();
        tweakGameAfterStarting(game);
        
        return game;
    }

    private static GabongGame CreateGame()
    {
        return GabongGame.Instantiate(new GabongGameCreatedEvent
        {
            Players = Some.FourPlayers(),
            Deck = TestDeck,
            InitialSeed = Some.Seed 
        });
    }

    private static List<Card> TestDeck => GabongDeck.Standard;
}