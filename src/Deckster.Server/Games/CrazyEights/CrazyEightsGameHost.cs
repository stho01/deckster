using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.CrazyEights.SampleClient;
using Deckster.Server.Communication;
using Deckster.Server.Data;
using Deckster.Server.Games.ChatRoom;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.Common.Fakes;
using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGameHost : GameHost<CrazyEightsRequest, CrazyEightsResponse, CrazyEightsNotification>
{
    public event Action<IGameHost>? OnEnded;
    public override string GameType => "CrazyEights";
    public override GameState State => _game.State;

    private CrazyEightsGame? _game;
    private readonly IRepo _repo;
    private IEventThing<CrazyEightsGame>? _events;
    private readonly List<CrazyEightsPoorAi> _bots = [];

    public CrazyEightsGameHost(IRepo repo)
    {
        _repo = repo;
    }

    public override bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (_players.Count >= 4)
        {
            error = "Too many players";
            return false;
        }

        if (!_players.TryAdd(channel.Player.Id, channel))
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

    private async void RequestReceived(IServerChannel channel, CrazyEightsRequest request)
    {
        if (_game == null || _game.State == GameState.Finished)
        {
            await channel.ReplyAsync(new FailureResponse("Game is not running"), JsonOptions);
            return;
        }

        await _game.HandleAsync(request);
        
        if (_game.State == GameState.Finished)
        {
            await _events.SaveChangesAsync();
            await _events.DisposeAsync();
            _events = null;
            _game = null;
                
            await Task.WhenAll(_players.Values.Select(p => p.WeAreDoneHereAsync()));
            await Cts.CancelAsync();
            Cts.Dispose();
            OnEnded?.Invoke(this);
        }
    }

    public override Task StartAsync()
    {
        if (_game != null)
        {
            return Task.CompletedTask;
        }

        var startEvent = new CrazyEightsGameCreatedEvent
        {
            Id = Guid.NewGuid(),
            Players = _players.Values.Select(p => p.Player).ToList(),
            Deck = Decks.Standard.KnuthShuffle(DateTimeOffset.UtcNow.Nanosecond)
        }.WithCommunicationContext(this);
        
        _game = CrazyEightsGame.Create(startEvent);
        _events = _repo.StartEventStream<CrazyEightsGame>(_game.Id, startEvent);
        _events.Append(startEvent);
        foreach (var player in _players.Values)
        {
            player.Start<CrazyEightsRequest>(RequestReceived, JsonOptions, Cts.Token);
        }

        return _game.StartAsync();
    }
}