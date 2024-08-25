using Deckster.Client.Common;
using Deckster.Client.Games.Uno;
using Deckster.Client.Sugar;

namespace Deckster.CrazyEights.SampleClient;

public class UnoNoob
{
    public UnoClient client { get; set; }
    
    public UnoNoob(UnoClient client)
    {
        this.client = client;
    }


    public void StartPlaying()
    {
        client.GameStarted += OnGameStarted;
        client.RoundStarted += OnRoundStarted;
        client.ItsYourTurn += OnItsYourTurn;
        client.PlayerPutCard += OnPlayerPutCard;
        client.PlayerPutWild += OnPlayerPutWild;
        client.PlayerDrewCard += OnPlayerDrewCard;
        client.PlayerPassed += OnPlayerPassed;
        client.RoundEnded += OnRoundEnded;
        client.GameEnded += OnGameEnded;

        client.SignalReadiness();
    }

    private void OnGameEnded(GameEndedMessage obj)
    {
        Console.WriteLine("==> Game ended");
    }

    private void OnRoundEnded(RoundEndedMessage obj)
    {
        Console.WriteLine("==> Round ended");
    }

    private void OnPlayerPassed(PlayerPassedMessage obj)
    {
        Console.WriteLine($"==> {obj.Player.Name} passed");
    }

    private void OnPlayerDrewCard(PlayerDrewCardMessage obj)
    {
        Console.WriteLine($"==> {obj.Player.Name} Drew");
    }

    private void OnPlayerPutWild(PlayerPutWildMessage obj)
    {
        Console.WriteLine($"==> {obj.Player.Name} Played {obj.Card} and changed color to {obj.NewColor}");
    }

    private void OnPlayerPutCard(PlayerPutCardMessage obj)
    {
        Console.WriteLine($"==> {obj.Player.Name} Played {obj.Card}");
    }

    private void OnItsYourTurn(ItsYourTurnMessage obj)
    {
        Console.WriteLine($"==> Heads up! It's your turn");
        DoSomethingInteractive(obj).Wait();


    }

    private async Task DoSomethingInteractive(ItsYourTurnMessage obj)
    {
        Console.WriteLine($"Top card is {obj.PlayerViewOfGame.TopOfPile}");
        Console.WriteLine("Current Color: " + obj.PlayerViewOfGame.CurrentSuit);
        Console.WriteLine("Write to play:");
        for(int i = 0; i<obj.PlayerViewOfGame.Cards.Count; i++)
        {
            Console.WriteLine($"{i+1}: {obj.PlayerViewOfGame.Cards[i]}");
        }

        Console.WriteLine("d: -draw card");
        Console.WriteLine("p: -pass turn");
        var action = Console.ReadLine();
        if (action == "d")
        {
            var cardDrawn = await client.DrawCardAsync();
            obj.PlayerViewOfGame.Cards.Add(cardDrawn);
            await DoSomethingInteractive(obj);
            return;
        }
        else if (action == "p")
        {
            var response = await client.PassAsync();
            if (response is FailureResponse)
            {
                await DoSomethingInteractive(obj);
                return;
            }
        }
        var cardIndex = action.TryParseToInt();
        if (!cardIndex.HasValue || cardIndex < 1 || cardIndex > obj.PlayerViewOfGame.Cards.Count)
        {
            await DoSomethingInteractive(obj);
            return;
        }

        var cardToPlay = obj.PlayerViewOfGame.Cards[cardIndex.Value - 1];
        if (cardToPlay.Color == UnoColor.Wild)
        {
            Console.WriteLine("Choose color: r, y, g, b");
            var color = Console.ReadLine();
            if (color != "r" && color != "y" && color != "g" && color != "b")
            {
                await DoSomethingInteractive(obj);
                return;
            }
            var colorEnum = color switch
            {
                "r" => UnoColor.Red,
                "y" => UnoColor.Yellow,
                "g" => UnoColor.Green,
                "b" => UnoColor.Blue,
                _ => throw new Exception("Invalid color")
            };
            var wildResult = await client.PutWildAsync(cardToPlay, colorEnum);
            if (wildResult is FailureResponse)
            {
                await DoSomethingInteractive(obj);
            }
        }
        else
        {
            var putResult = await client.PutCardAsync(cardToPlay);
            if (putResult is FailureResponse)
            {
                await DoSomethingInteractive(obj);
            }
        }
    }

    private void OnRoundStarted(RoundStartedMessage obj)
    {
        Console.WriteLine($"==> Round started");
    }

    private void OnGameStarted(GameStartedMessage obj)
    {
        Console.WriteLine($"==> Game started");
    }
}