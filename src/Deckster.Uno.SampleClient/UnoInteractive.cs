using Deckster.Client.Games.Uno;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Uno;

namespace Deckster.Uno.SampleClient;

public class UnoInteractive
{
    private PlayerViewOfGame _playerViewOfGame = new();
    private readonly TaskCompletionSource _tcs = new();

    private readonly UnoClient _client;
    
    public UnoInteractive(UnoClient client)
    {
        _client = client;
        client.GameStarted += OnGameStarted;
        client.RoundStarted += OnRoundStarted;
        client.ItsYourTurn += OnItsYourTurn;
        client.PlayerPutCard += OnPlayerPutCard;
        client.PlayerPutWild += OnPlayerPutWild;
        client.PlayerDrewCard += OnPlayerDrewCard;
        client.PlayerPassed += OnPlayerPassed;
        client.RoundEnded += OnRoundEnded;
        client.GameEnded += OnGameEnded;
    }

    public Task PlayAsync() => _tcs.Task;

    private void OnGameEnded(GameEndedNotification obj)
    {
        Console.WriteLine("==> Game ended");
        _tcs.SetResult();
    }

    private void OnRoundEnded(RoundEndedNotification obj)
    {
        Console.WriteLine("==> Round ended");
    }

    private void OnPlayerPassed(PlayerPassedNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} passed");
    }

    private OtherUnoPlayer? GetPlayer(Guid playerId)
    {
        return _playerViewOfGame.OtherPlayers.FirstOrDefault(p => p.Id == playerId);
    }

    private void OnPlayerDrewCard(PlayerDrewCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Drew");
    }

    private void OnPlayerPutWild(PlayerPutWildNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Played {obj.Card} and changed color to {obj.NewColor}");
    }

    private void OnPlayerPutCard(PlayerPutCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Played {obj.Card}");
    }

    private async void OnItsYourTurn(ItsYourTurnNotification obj)
    {
        Console.WriteLine("==> Heads up! It's your turn");
        await DoSomethingInteractive(obj);
    }

    private async Task DoSomethingInteractive(ItsYourTurnNotification obj)
    {
        Console.WriteLine($"Top card is {obj.PlayerViewOfGame.TopOfPile}");
        Console.WriteLine("Current Color: " + obj.PlayerViewOfGame.CurrentColor);
        Console.WriteLine("Write to play:");
        for(int i = 0; i<obj.PlayerViewOfGame.Cards.Count; i++)
        {
            Console.WriteLine($"{i+1}: {obj.PlayerViewOfGame.Cards[i]}");
        }

        Console.WriteLine("d: -draw card");
        Console.WriteLine("p: -pass turn");
        var action = Console.ReadLine();
        switch (action)
        {
            case "d":
            {
                var cardDrawn = await _client.DrawCardOrThrowAsync();
                obj.PlayerViewOfGame.Cards.Add(cardDrawn);
                await DoSomethingInteractive(obj);
                return;
            }
            case "p":
                await _client.PassAsync();
                await DoSomethingInteractive(obj);
                return;
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
            var wildResult = await _client.PutWildAsync(cardToPlay, colorEnum);
            if (wildResult is EmptyResponse)
            {
                await DoSomethingInteractive(obj);
            }
        }
        else
        {
            var putResult = await _client.PutCardAsync(cardToPlay);
            if (putResult is EmptyResponse)
            {
                await DoSomethingInteractive(obj);
            }
        }
    }

    private void OnRoundStarted(RoundStartedNotification obj)
    {
        Console.WriteLine("==> Round started");
    }

    private void OnGameStarted(GameStartedNotification notification)
    {
        _playerViewOfGame = notification.PlayerViewOfGame;
        Console.WriteLine("==> Game started");
    }
}