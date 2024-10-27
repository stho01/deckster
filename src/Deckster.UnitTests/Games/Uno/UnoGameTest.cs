using Deckster.Client.Games.Common;
using Deckster.Client.Games.Uno;
using Deckster.Client.Serialization;
using Deckster.Server.Collections;
using Deckster.Server.Games.Uno.Core;
using NUnit.Framework;

namespace Deckster.UnitTests.Games.Uno;

public class UnoGameTest
{
    [Test]
    public async ValueTask PutCard_Succeeds()
    {
        var game = SetUpGame(g =>
        {
            var cards = g.Deck;
            g.CurrentPlayer.Cards.Add(cards.Get(UnoValue.Eight, UnoColor.Blue));
            g.DiscardPile.Push(cards.Get(UnoValue.Seven, UnoColor.Blue));
        });
        
        var card = new UnoCard(UnoValue.Eight, UnoColor.Blue);
        var result = await game.PutCard(game.CurrentPlayer.Id, card);
        Asserts.Success(result);
    }

    [Test]
    public async ValueTask PutCard_Fails_WhenNotYourTurn()
    {
        var game = CreateGame();
        var player = game.Players[1];
        var result = await game.PutCard(player.Id, player.Cards[0]);

        Asserts.Fail(result, "It is not your turn");
    }
    
    [Test]
    [TestCase(UnoValue.One, UnoColor.Red, "You don't have 'One Red'")]
    [TestCase(UnoValue.Seven, UnoColor.Blue, "Cannot put 'Seven Blue' on 'One Green'")]
    public async Task PutCard_Fails(UnoValue value, UnoColor color, string errorMessage)
    {
        var game = SetUpGame(g =>
        {
            var cards = g.Deck;
            g.Players[0].Cards.Add(cards.Get(UnoValue.Eight, UnoColor.Blue));
            g.Players[0].Cards.Add(cards.Get(UnoValue.Seven, UnoColor.Blue));
            
            g.Players[1].Cards.Add(cards.Get(UnoValue.Eight, UnoColor.Red));
            
            g.DiscardPile.Push(cards.Get(UnoValue.One, UnoColor.Green));
        });
        var player = game.Players[0];
        var card = new UnoCard(value, color);
        
        var result = await game.PutCard(player.Id, card);
        Asserts.Fail(result, errorMessage);
    }
    
    [Test]
    public async Task DrawCard_Fails_AfterThreeAttempts()
    {
        var game = CreateGame();
        var player = game.Players[0];

        for (var ii = 0; ii < 2; ii++)
        {
            await game.DrawCard(player.Id);
        }
        
        var result = await game.DrawCard(player.Id);
        Asserts.Fail(result, "You can only draw 1 card, then pass if you can't play");
    }
    
    [Test]
    public async Task Pass_Fails_WhenCardIsNotDrawn()
    {
        var game = CreateGame();
        var player = game.Players[0];
        var result = await game.Pass(player.Id);
        Asserts.Fail(result, "You have to draw a card first");
    }
    
    [Test]
    public async Task Pass_SucceedsAlways()
    {
        var game = CreateGame();
        var player = game.Players[0];
        Asserts.Success(await game.DrawCard(player.Id));
        var result = await game.Pass(player.Id);
        Asserts.Success(result);
    }
    
    [Test]
    [TestCase(UnoColor.Blue)]
    [TestCase(UnoColor.Red)]
    [TestCase(UnoColor.Green)]
    [TestCase(UnoColor.Yellow)]
    public async Task PutWild_ChangesColor(UnoColor newColor)
    {
        var game = SetUpGame(g =>
        {
            var cards = TestDeck;
            g.Players[0].Cards.Add(cards.Get(UnoValue.Wild, UnoColor.Wild));
            g.Players[0].Cards.Add(cards.Get(UnoValue.Eight, UnoColor.Red));
            
            g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Blue));
            g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Red));
            g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Green));
            g.Players[1].Cards.Add(cards.Get(UnoValue.Nine, UnoColor.Yellow));
            
            g.DiscardPile.Push(cards.Get(UnoValue.Seven, UnoColor.Blue));
        });
        var player = game.CurrentPlayer;
        var eight = new UnoCard(UnoValue.Wild, UnoColor.Wild);
        Asserts.Success(await game.PutWild(player.Id, eight, newColor));
        Assert.That(game.CurrentColor, Is.EqualTo(newColor));

        var nextPlayer = game.CurrentPlayer;
        var cardWithNewSuit = nextPlayer.Cards.First(c => c.Color == newColor);

        Asserts.Success(await game.PutCard(nextPlayer.Id, cardWithNewSuit));
    }
    
    [Test]
    public async Task PutWild_Fails_WhenNotWild()
    {
        var game = SetUpGame(g =>
        {
            var deck = g.Deck;
            g.Players[0].Cards.Add(deck.Get(UnoValue.Eight, UnoColor.Blue));
        });
        var player = game.Players[0];
        var notWild = new UnoCard(UnoValue.Eight, UnoColor.Blue);
        var result = await game.PutWild(player.Id, notWild, Some.UnoColor);
        Asserts.Fail(result, "Eight Blue is not a wildcard");
    }
    
    [Test]
    public async Task Draw_Fails_WhenNoMoreCards()
    {
        var game = CreateGame();
        game.StockPile.Clear();
        
        var result = await game.DrawCard(game.CurrentPlayer.Id);
        Console.WriteLine(result.Pretty());
        Asserts.Fail(result, "No more cards");
    }
    
    private static UnoGame SetUpGame(Action<UnoGame> configure)
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

        var game = UnoGame.Create(new UnoGameCreatedEvent
        {
            Id = Some.Id,
            Players = players,
            Deck = TestDeck
        });

        configure(game);
        
        return game;
    }

    private static UnoGame CreateGame()
    {
        return UnoGame.Create(new UnoGameCreatedEvent
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

    private static List<UnoCard> TestDeck => UnoDeck.Standard;
}