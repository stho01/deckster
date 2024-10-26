using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.CrazyEights.SampleClient;
using Deckster.Server.Communication;
using Deckster.Server.Data;
using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.Common.Fakes;
using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGameHost : GameHost
{
    private readonly Locked<CrazyEightsGame> _game = new();
    private IEventQueue<CrazyEightsGame>? _events;
    
    public event Action<IGameHost>? OnEnded;
    public override string GameType => "CrazyEights";
    public override GameState State => _game.Value?.State ?? GameState.Waiting;

    private readonly IRepo _repo;
    private readonly List<CrazyEightsPoorAi> _bots = [];

    public CrazyEightsGameHost(IRepo repo)
    {
        _repo = repo;
    }

    public override bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (Players.Count >= 4)
        {
            error = "Too many players";
            return false;
        }

        if (!Players.TryAdd(channel.Player.Id, channel))
        {
            error = "Could not add player";
            return false;
        }

        error = default;
        return true;
    }

    public override bool TryAddBot([MaybeNullWhen(true)] out string error)
    {
        var channel = new InMemoryChannel
        {
            Player = new PlayerData
            {
                Id = Guid.NewGuid(),
                Name = TestNames.Random()
            }
        };
        var bot = new CrazyEightsPoorAi(new CrazyEightsClient(channel));
        _bots.Add(bot);
        return TryAddPlayer(channel, out error);
    }

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly SemaphoreSlim _endSemaphore = new(1, 1);

    private async void RequestReceived(IServerChannel channel, CrazyEightsRequest request)
    {
        await _semaphore.WaitAsync();
        var game = _game.Value;
        var events = _events;
        try
        {
            if (game == null || game.State == GameState.Finished)
            {
                await channel.ReplyAsync(new FailureResponse("Game is not running"), JsonOptions);
                return;
            }

            switch (request)
            {
                case PutCardRequest put:
                    await CrazyEightsProjection.Apply(put, game);
                    break;
                case PutEightRequest put:
                    await CrazyEightsProjection.Apply(put, game);
                    break;
                case DrawCardRequest put:
                    await CrazyEightsProjection.Apply(put, game);
                    break;
                case PassRequest put:
                    await CrazyEightsProjection.Apply(put, game);
                    break;
                default:
                    await channel.ReplyAsync(new FailureResponse($"Unsupported request: '{request.GetType().Name}'"), JsonOptions);
                    return;    
                
            }
            
            // if (!await CrazyEightsProjection.HandleAsync(request, game))
            // {
            //     await channel.ReplyAsync(new FailureResponse($"Unsupported request: '{request.GetType().Name}'"), JsonOptions);
            //     return;
            // }

            
            if (events == null)
            {
                var wat = true;
            }
            events?.Append(request);
        }
        finally
        {
            _semaphore.Release();
        }
        
        
        if (game.State == GameState.Finished)
        {
            await _endSemaphore.WaitAsync();
            try
            {
                if (_game.Value == null)
                {
                    return;
                }
                _game.Value = null;
            
                if (events != null)
                {
                    await events.FlushAsync();
                    await events.DisposeAsync();
                }
                
                await EndAsync(game.Id);
            }
            finally
            {
                _endSemaphore.Release();
            }
        }
    }

    public override async Task StartAsync()
    {
        var game = _game.Value;
        if (game != null)
        {
            return;
        }

        var startEvent = new CrazyEightsGameCreatedEvent
        {
            Id = Guid.NewGuid(),
            Players = Players.Values.Select(p => p.Player).ToList(),
            Deck = Decks.Standard.KnuthShuffle(DateTimeOffset.UtcNow.Nanosecond)
        }
            .WithCommunicationContext(this);
        
        game = CrazyEightsProjection.Create(startEvent);
        // game.SetCommunicationContext(this);
        var events = _repo.StartEventQueue<CrazyEightsGame>(game.Id, startEvent);
        
        foreach (var player in Players.Values)
        {
            player.StartReading<CrazyEightsRequest>(RequestReceived, JsonOptions, Cts.Token);
        }

        _game.Value = game;
        _events = events;
        
        await game.StartAsync();
    }
}