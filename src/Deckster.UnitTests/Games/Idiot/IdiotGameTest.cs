using Deckster.Core.Collections;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Idiot;
using Deckster.Games.Collections;
using Deckster.Games.Idiot;
using Deckster.Server.Games;
using Deckster.UnitTests.Fakes;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Idiot;

public class IdiotGameTest
{
    [Test]
    public async ValueTask SwapCards()
    {
        var goodCard = new Card(1, Suit.Spades);
        var badCard = new Card(3, Suit.Clubs);
        
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.Steal(goodCard));
            g.Players[0].CardsFacingUp.Push(deck.Steal(badCard));
            
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.HasStarted = false;
        });
        var communication = new FakeCommunication();
        game.WireUp(communication);

        var player = game.Players[0];
        var response = await game.SwapCards(new SwapCardsRequest
        {
            PlayerId = player.Id,
            CardOnHand = goodCard,
            CardFacingUp = badCard
        });
        Asserts.Success(response);
        
        Assert.That(response.CardNowFacingUp, Is.EqualTo(goodCard));
        Assert.That(response.CardNowOnHand, Is.EqualTo(badCard));
        
        Assert.That(player.CardsFacingUp.Contains(goodCard));
        Assert.That(player.CardsOnHand.Contains(badCard));
        
        Assert.That(communication.HasBroadcasted<PlayerSwappedCardsNotification>());
    }

    [Test]
    public async ValueTask SwapCards_Fails_WhenPlayerDoesNotHaveCardOnHand()
    {
        var dontHave = new Card(1, Suit.Spades);
        var cardFacingUp = new Card(3, Suit.Clubs);
        
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            deck.Steal(dontHave);
            g.Players[0].CardsOnHand.Push(deck.StealRandom());
            g.Players[0].CardsFacingUp.Push(deck.Steal(cardFacingUp));
            
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.HasStarted = false;
        });
        var communication = new FakeCommunication();
        game.WireUp(communication);

        var player = game.Players[0];
        var response = await game.SwapCards(new SwapCardsRequest
        {
            PlayerId = player.Id,
            CardOnHand = dontHave,
            CardFacingUp = cardFacingUp
        });
        Asserts.Fail(response, "You don't have that card on hand");
    }
    
    [Test]
    public async ValueTask SwapCards_Fails_WhenPlayerDoesNotHaveCardFacingUp()
    {
        var cardOnHand = new Card(1, Suit.Spades);
        var dontHave = new Card(3, Suit.Clubs);
        
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            deck.Steal(dontHave);
            g.Players[0].CardsOnHand.Push(deck.Steal(cardOnHand));
            g.Players[0].CardsFacingUp.Push(deck.StealRandom());
            
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.HasStarted = false;
        });
        var communication = new FakeCommunication();
        game.WireUp(communication);

        var player = game.Players[0];
        var response = await game.SwapCards(new SwapCardsRequest
        {
            PlayerId = player.Id,
            CardOnHand = cardOnHand,
            CardFacingUp = dontHave
        });
        Asserts.Fail(response, "You don't have that card facing up");
    }

    [Test]
    public async ValueTask SwapCards_Fails_WhenGameHasStarted()
    {
        var cardOnHand = new Card(1, Suit.Spades);
        var dontHave = new Card(3, Suit.Clubs);
        
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            deck.Steal(dontHave);
            g.Players[0].CardsOnHand.Push(deck.Steal(cardOnHand));
            g.Players[0].CardsFacingUp.Push(deck.StealRandom());
            
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.HasStarted = true;
        });
        var communication = new FakeCommunication();
        game.WireUp(communication);

        var player = game.Players[0];
        var response = await game.SwapCards(new SwapCardsRequest
        {
            PlayerId = player.Id,
            CardOnHand = cardOnHand,
            CardFacingUp = dontHave
        });
        Asserts.Fail(response, "Game has started");
    }

    [Test]
    public async ValueTask IamReady_SetsPlayerReady()
    {
        var game = SetUpGame(g =>
        {
            g.Deal();
            g.HasStarted = false;
        });

        var response = await game.IamReady(new IamReadyRequest {PlayerId = game.Players[0].Id});
        Asserts.Success(response);
        Assert.That(game.Players[0].IsReady);
    }

    [Test]
    public async ValueTask IamReady_StartsGame_WhenAllPlayersAreReady()
    {
        var game = SetUpGame(g =>
        {
            g.Deal();
            g.HasStarted = false;
        });
        var communication = new FakeCommunication();
        game.WireUp(communication);
        foreach (var player in game.Players)
        {
            await game.IamReady(new IamReadyRequest {PlayerId = player.Id});
        }
        
        Assert.That(game.Players.All(p => p.IsReady));
        Assert.That(game.HasStarted);
        Assert.That(communication.HasBroadcasted<GameStartedNotification>());
    }
    
    [Test]
    public async ValueTask PutCards()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.Steal(8, Suit.Spades));
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
        });

        var response = await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [new Card(8, Suit.Spades)]});
        Asserts.Success(response);
    }

    [Test]
    public async ValueTask PutCards_Fails_WhenPlayerDoesNotHaveCards()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.Steal(8, Suit.Spades));
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
        });

        var response = await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [new Card(9, Suit.Spades)]});
        Asserts.Fail(response, "You don't have those cards");
    }

    [Test]
    public async ValueTask PutCards_Fails_WhenNoCards()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
        });

        var response = await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [] });
        Asserts.Fail(response, "You must put at least 1 card");
    }

    [Test]
    public async ValueTask PutCards_Fails_WhenCardsHaveDifferentRanks()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsOnHand.Push(deck.Steal(9, Suit.Spades));
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
        });

        var response = await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [new Card(8, Suit.Spades), new Card(9, Suit.Spades)]});
        Asserts.Fail(response, "All cards must have same rank");
    }
    
    [Test]
    public async ValueTask PutCards_Fails_WhenRankIsLowerThanTopOfPile()
    {
        var game = SetUpGame(g =>
        {
            
            var pile = g.DiscardPile;
            pile.PushRange(g.Deck);
            g.DiscardPile.Push(pile.Steal(11, Suit.Spades));
            
            g.Players[0].CardsOnHand.Push(pile.Steal(8, Suit.Spades));
            g.Players[0].CardsOnHand.Push(pile.StealRandom());
            g.Players[1].CardsOnHand.Push(pile.StealRandom());
            g.Players[2].CardsOnHand.Push(pile.StealRandom());
        });

        var response = await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [new Card(8, Suit.Spades)] });
        Asserts.Fail(response, "Rank (8) must be equal to or higher than current rank (11)");
    }
    
    [Test]
    [TestCase(10, Suit.Spades)]
    [TestCase(2, Suit.Spades)]
    public async ValueTask PutSpecialCard_AlwaysWorks(int rank, Suit suit)
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.Steal(10, Suit.Spades));
            g.Players[0].CardsOnHand.Push(deck.Steal(2, Suit.Spades));
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            
            g.DiscardPile.Push(deck.Steal(12, Suit.Spades));
        });

        Asserts.Success(await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [new Card(rank, suit)]}));
    }
    
    [Test]
    public async ValueTask PutTen_FlushesDiscardPile()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.Steal(10, Suit.Spades));
            g.Players[0].CardsOnHand.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
        });

        Asserts.Success(await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [new Card(10, Suit.Spades)]}));
        Assert.That(game.DiscardPile, Is.Empty);
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[0]));
    }
    
    [Test]
    public async ValueTask PutFourOfSameRank_FlushesDiscardPile()
    {
        Card[] cards = [new(7, Suit.Spades), new(7, Suit.Clubs), new(7, Suit.Hearts), new(7, Suit.Diamonds)];
        
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            foreach (var card in cards)
            {
                g.Players[0].CardsOnHand.Push(deck.Steal(card));    
            }
            
            g.Players[0].CardsOnHand.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
        });
        
        Asserts.Success(await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = cards }));
        Assert.That(game.DiscardPile, Is.Empty);
        
        
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[0]));
    }

    [Test]
    [TestCase(8, Suit.Spades)]
    [TestCase(2, Suit.Spades)]
    [TestCase(10, Suit.Spades)]
    public async ValueTask PutLastCard_MovesToNextPlayer_RegardlessOfCard(int rank, Suit suit)
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsOnHand.Push(deck.Steal(rank, suit));
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
        });

        Asserts.Success(await game.PutCardsFromHand(new PutCardsFromHandRequest{ PlayerId = game.CurrentPlayer.Id, Cards = [new Card(rank, suit)] }));
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[1]));
    }

    [Test]
    public async ValueTask PutCardFacingDown()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsFacingDown.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.DiscardPile.Clear();
            g.LastCardPutBy = g.Players[0].Id;
        });

        var response = await game.PutCardFacingDown(new PutCardFacingDownRequest {PlayerId = game.CurrentPlayer.Id, Index = 0});
        Asserts.Success(response);
        
        Assert.That(response.AttemptedCard, Is.EqualTo(new Card(8, Suit.Spades)));
        Assert.That(response.PullInCards, Is.Empty);
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[1]));
    }

    [Test]
    public async ValueTask PutCardFacingDown_ReturnsDiscardPileAndCard_WhenCardIsTooLow()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            var topofPile = deck.Steal(9, Suit.Spades);
            g.Players[0].CardsFacingDown.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            
            g.DiscardPile.PushRange(deck.StealAll());
            g.DiscardPile.Push(topofPile);
        });
        var discardPile = game.DiscardPile.ToList();
        var player = game.Players[0];

        var response = await game.PutCardFacingDown(new PutCardFacingDownRequest {PlayerId = game.CurrentPlayer.Id, Index = 0});
        Asserts.Success(response);
        
        Assert.That(response.AttemptedCard, Is.EqualTo(new Card(8, Suit.Spades)));
        Assert.That(response.PullInCards.ContainsAll(discardPile));
        
        Assert.That(player.CardsOnHand.Contains(9, Suit.Spades));
        Assert.That(player.CardsOnHand.ContainsAll(discardPile));
    }
    
    [Test]
    public async ValueTask PutCardFacingDown_Fails_WhenNotYourTurn()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsFacingDown.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.StockPile.PushRange(deck);
            g.DiscardPile.Clear();
            g.LastCardPutBy = g.Players[0].Id;
        });

        Asserts.Fail(await game.PutCardFacingDown(new PutCardFacingDownRequest{ PlayerId = game.Players[1].Id, Index = 0 }),
            "It is not your turn");
    }

    [Test]
    public async ValueTask PutCardFacingDown_Fails_ForInvalidIndex()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsFacingDown.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.DiscardPile.Clear();
            g.LastCardPutBy = g.Players[0].Id;
        });

        Asserts.Success(await game.PutCardFacingDown(new PutCardFacingDownRequest{ PlayerId = game.CurrentPlayer.Id, Index = 0 }));
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[1]));
    }
    
    [Test]
    public async ValueTask PutCardFacingDown_Fails_WhenStockPileHasCards()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].CardsFacingDown.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            g.StockPile.PushRange(deck);
            g.DiscardPile.Clear();
            g.LastCardPutBy = g.Players[0].Id;
        });

        Asserts.Fail(await game.PutCardFacingDown(new PutCardFacingDownRequest{ PlayerId = game.CurrentPlayer.Id, Index = 0 }),
            "There are still cards in stock pile");
    }

    [Test]
    public async ValueTask PutChanceCard()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;

            var chanceCard = deck.Steal(1, Suit.Hearts);
            var topOfPile = deck.Steal(1, Suit.Diamonds);
            
            g.Players[0].CardsOnHand.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsOnHand.Push(deck.Steal(9, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            
            g.DiscardPile.Push(topOfPile);
            g.StockPile.Push(chanceCard);
        });

        var response = await game.PutChanceCard(new PutChanceCardRequest {PlayerId = game.CurrentPlayer.Id});
        Asserts.Success(response);
        
        Assert.That(response.AttemptedCard, Is.EqualTo(new Card(1, Suit.Hearts)));
        Assert.That(response.PullInCards, Is.Empty);
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[1]));
    }

    [Test]
    public async ValueTask PutChanceCard_PullsIn_WhenCardIsTooLow()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;

            var chanceCard = deck.Steal(3, Suit.Hearts);
            var topOfPile = deck.Steal(1, Suit.Diamonds);
            
            g.Players[0].CardsOnHand.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsOnHand.Push(deck.Steal(9, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            
            g.DiscardPile.Push(topOfPile);
            g.StockPile.Push(chanceCard);
        });

        var response = await game.PutChanceCard(new PutChanceCardRequest {PlayerId = game.CurrentPlayer.Id});
        Asserts.Success(response);
        
        Assert.That(response.AttemptedCard, Is.EqualTo(new Card(3, Suit.Hearts)));
        Assert.That(response.PullInCards, Is.EquivalentTo(new []{new Card(1, Suit.Diamonds), new Card(3, Suit.Hearts)}));
        Assert.That(game.CurrentPlayer, Is.SameAs(game.Players[1]));
    }

    [Test]
    public async ValueTask PutChanceCard_Fails_WhenPlayerCanPlay()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;

            var topOfPile = deck.Steal(7, Suit.Diamonds);
            g.Players[0].CardsOnHand.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsOnHand.Push(deck.Steal(9, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            
            g.DiscardPile.Push(topOfPile);
            g.StockPile.PushRange(deck.StealAll());
        });

        var response = await game.PutChanceCard(new PutChanceCardRequest {PlayerId = game.CurrentPlayer.Id});
        Asserts.Fail(response, "You must play one of your cards");
    }
    
    [Test]
    public async ValueTask PutChanceCard_Fails_WhenStockPileIsEmpty()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            
            var topOfPile = deck.Steal(11, Suit.Diamonds);
            g.Players[0].CardsOnHand.Push(deck.Steal(8, Suit.Spades));
            g.Players[0].CardsOnHand.Push(deck.Steal(9, Suit.Spades));
            g.Players[0].CardsFacingDown.Push(deck.StealRandom());
            g.Players[1].CardsOnHand.Push(deck.StealRandom());
            g.Players[2].CardsOnHand.Push(deck.StealRandom());
            
            g.DiscardPile.Push(topOfPile);
        });

        var response = await game.PutChanceCard(new PutChanceCardRequest {PlayerId = game.CurrentPlayer.Id});
        Asserts.Fail(response, "Stock pile is empty");
    }
    
    private static IdiotGame SetUpGame(Action<IdiotGame> configure)
    {
        
        var game = IdiotGame.Create(new IdiotGameCreatedEvent
        {
            Id = Some.Id,
            Players = Some.FourPlayers(),
            Deck = Decks.Standard()
        });
        game.HasStarted = true;
        configure(game);
        
        return game;
    }
}
