using Deckster.Client.Games.Gabong;
using Deckster.Core.Games.Common;
using Deckster.Core.Games.Gabong;

namespace Deckster.Gabong.SampleClient;

public class GabongInteractive
{
    private PlayerViewOfGame _playerViewOfGame = new();
    private readonly TaskCompletionSource _tcs = new();

    private List<string> _messages = new();
    private readonly GabongClient _client;
    
    public GabongInteractive(GabongClient client)
    {
        _client = client;
        client.GameStarted += OnGameStarted;
        client.RoundStarted += OnRoundStarted;
        client.PlayerPutCard += OnPlayerPutCard;
        client.PlayerDrewCard += OnPlayerDrewCard;
        client.PlayerLostTheirTurn += OnPlayerLostTheirTurn;
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

    private void OnPlayerLostTheirTurn(PlayerLostTheirTurnNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} lost their turn");
    }

    private OtherGabongPlayer? GetPlayer(Guid playerId)
    {
        return _playerViewOfGame.OtherPlayers.FirstOrDefault(p => p.Id == playerId);
    }

    private void OnPlayerDrewCard(PlayerDrewCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Drew");
    }

    private void OnPlayerPutCard(PlayerPutCardNotification obj)
    {
        Console.WriteLine($"==> {GetPlayer(obj.PlayerId)} Played {obj.Card}");
    }

    private async Task DoSomethingInteractive(PlayerViewOfGame? obj)
    {
        if (obj != null)
        {
            _playerViewOfGame = obj;
            _messages = new List<string>();
        }
       
        Console.WriteLine("----");
        Console.WriteLine($"Top card id {_playerViewOfGame.TopOfPile}");
        Console.WriteLine("Current Suit is " + _playerViewOfGame.CurrentSuit);
        if (_playerViewOfGame.LastPlay == GabongPlay.RoundStarted)
        {
            Console.WriteLine($"Starting player is {GetPlayer(_playerViewOfGame.LastPlayMadeByPlayerId)}");
        }
        else
        {
            Console.WriteLine($"Last play ({_playerViewOfGame.LastPlay}) was made by {GetPlayer(_playerViewOfGame.LastPlayMadeByPlayerId)}");
        }
        if (_messages.Any())
        {
            Console.WriteLine("And then this happened:");
            foreach (var message in _messages.ToList())
            {
                Console.WriteLine(message);
            }
        }
        Console.WriteLine("----");
        Console.WriteLine("Write to play:");
        for(int i = 0; i<obj.Cards.Count; i++)
        {
            Console.WriteLine($"{i+1}: {obj.Cards[i]}");
        }

        Console.WriteLine("d: -draw card");
        Console.WriteLine("p: -pass turn");
        Console.WriteLine("g: -play gabong");
        Console.WriteLine("b: -play bonga");
        Console.WriteLine("-- or just press enter to await more info");
        var action = Console.ReadLine();
        switch (action)
        {
            case "":
                await DoSomethingInteractive(obj);
                return;
            case "d":
            {
                var playerView = await _client.DrawCardAsync(new DrawCardRequest());
                await DoSomethingInteractive(playerView);
                return;
            }
            case "p":
                await _client.PassAsync();
                await DoSomethingInteractive(obj);
                return;
            case "g":
                await _client.PlayGabongAsync();
                await DoSomethingInteractive(obj);
                return;
            case "b":
                await _client.PlayBongaAsync();
                await DoSomethingInteractive(obj);
                return;
        }
        var cardIndex = action.TryParseToInt();
        if (!cardIndex.HasValue || cardIndex < 1 || cardIndex > obj.Cards.Count)
        {
            await DoSomethingInteractive(obj);
            return;
        }

        var cardToPlay = obj.Cards[cardIndex.Value - 1];
        if (cardToPlay.Rank == 8)
        {
            Console.WriteLine("Choose color: c, h, s, d");
            var color = Console.ReadLine();
            if (color != "c" && color != "h" && color != "s" && color != "d")
            {
                await DoSomethingInteractive(obj);
                return;
            }
            var suit = color switch
            {
                "c" => Suit.Clubs,
                "h" => Suit.Hearts,
                "s" => Suit.Spades,
                "d" => Suit.Diamonds,
                _ => throw new Exception("Invalid suit")
            };
            _playerViewOfGame = await _client.PutCardAsync(new PutCardRequest{Card = cardToPlay, NewSuit = suit });
            await DoSomethingInteractive(_playerViewOfGame);
        }
        else
        {
            var putResult = await _client.PutCardAsync(new PutCardRequest{Card = cardToPlay, NewSuit = null});
            await DoSomethingInteractive(putResult);
        }
    }

    private void OnRoundStarted(RoundStartedNotification obj)
    {
        _playerViewOfGame = obj.PlayerViewOfGame;
        Console.WriteLine("==> Round started. Starting player is "+GetPlayer(obj.StartingPlayerId));
    }

    private void OnGameStarted(GameStartedNotification notification)
    {
        Console.WriteLine("==> Game started, waiting for round start");
    }
}