using System.Collections.Concurrent;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Logging;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsGameHost
{
    private readonly ILogger _logger;
    public bool IsStarted => _game != null;
    public Guid Id { get; } = Guid.NewGuid();

    private CrazyEightsGame? _game;
    private readonly CrazyEightsRepo _repo;
    private readonly ConcurrentDictionary<Guid, IDecksterChannel> _channels = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public CrazyEightsGameHost(CrazyEightsRepo repo)
    {
        _logger = Log.Factory.CreateLogger($"{nameof(CrazyEightsGameHost)} {Id}");
        _repo = repo;
    }

    public void Add(IDecksterChannel channel)
    {
        if (IsStarted)
        {
            return;
        }

        channel.OnMessage += OnMessage;
        channel.OnDisconnected += OnDisconnected;
        _channels[channel.PlayerData.PlayerId] = channel;
    }

    private Task OnDisconnected(IDecksterChannel channel)
    {
        _logger.LogInformation("{player} disconnected", channel.PlayerData.Name);
        _channels.Remove(channel.PlayerData.PlayerId, out _);
        return Task.CompletedTask;
    }

    private async void OnMessage(IDecksterChannel channel, byte[] bytes)
    {
        var command = DecksterJson.Deserialize<CrazyEightsCommand>(bytes);
        try
        {
            await _semaphore.WaitAsync();
            await (command switch
            {
                null => channel.RespondAsync(new FailureResult("Troll command")),
                StartCommand m => StartAsync(channel, m),
                _ => PerformMoveAsync(channel, command)
            });
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task PerformMoveAsync(IDecksterChannel channel, CrazyEightsCommand command)
    {
        try
        {
            if (_game == null)
            {
                await channel.RespondAsync(new FailureResult("Game has not started yet."));
                return;
            }

            if (_game.State == GameState.Finished)
            {
                await channel.RespondAsync(new FailureResult("Game is finished."));
                return;
            }

            var playerId = channel.PlayerData.PlayerId;
            var result = command switch
            {
                PutCardCommand m => _game.PutCard(playerId, m.Card),
                PutEightCommand m => _game.PutEight(playerId, m.Card, m.NewSuit),
                DrawCardCommand m => _game.DrawCard(playerId),
                PassCommand m => _game.Pass(playerId),
                _ => new FailureResult("Unknown command")
            };
            _logger.LogInformation("Result for {name}: {type}", channel.PlayerData.Name, result.GetType().Name);

            await channel.RespondAsync(result);

            if (result is SuccessResult)
            {
                await BroadcastFromAsync(playerId, CreateBroadcastMessage(playerId, command));
                switch (_game.State)
                {
                    case GameState.Finished:
                        await Task.WhenAll(_channels.Select(c => c.Value.SendAsync(
                            new GameEndedMessage
                            {
                                Players = _game.Players.Select(p => new PlayerData
                                {
                                    PlayerId = p.Id,
                                    Name = p.Name
                                }).ToList()
                            }))
                        );
                        return;
                    default:
                    {
                        var currentPlayerId = _game.CurrentPlayer.Id;
                        if (currentPlayerId == playerId)
                        {
                            return;
                        }

                        var state = _game.GetStateFor(currentPlayerId);
                        await _channels[currentPlayerId].SendAsync(new ItsYourTurnMessage {PlayerViewOfGame = state});
                        return;
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled");
        }
    }

    private Task BroadcastFromAsync<TMessage>(Guid playerId, TMessage message) where TMessage : CrazyEightsMessage
    {
        var communiactors = _channels.Values.Where(c => c.PlayerData.PlayerId != playerId);
        return Task.WhenAll(communiactors.Select(c => c.SendAsync<CrazyEightsMessage>(message)));
    }
    
    private static CrazyEightsMessage CreateBroadcastMessage(Guid playerId, CrazyEightsCommand command)
    {
        return command switch
        {
            PutCardCommand p => new PlayerPutCardMessage {PlayerId = playerId, Card = p.Card},
            PutEightCommand p => new PlayerPutEightMessage {PlayerId = playerId, Card = p.Card, NewSuit = p.NewSuit},
            DrawCardCommand p => new PlayerDrewCardMessage {PlayerId = playerId},
            PassCommand p => new PlayerPassedMessage {PlayerId = playerId},
            _ => throw new Exception($"Unknown broadcast message for '{command.GetType().Name}'")
        };
    }

    private async Task StartAsync(IDecksterChannel channel, StartCommand command)
    {
        var result = CreateGame();
        await channel.RespondAsync(result);
    
        if (result is SuccessResult)
        {
            await StartGameAsync();    
        }
    }

    public async Task<CommandResult> StartAsync(CancellationToken cancellationToken = default)
    {
        var result = CreateGame();
        if (result is SuccessResult)
        {
            await StartGameAsync(cancellationToken);    
        }

        return result;
    }

    private async Task StartGameAsync(CancellationToken cancellationToken = default)
    {
        if (_game == null)
        {
            return;
        }
        
        await Task.WhenAll(_channels.Select(c => c.Value.SendAsync(
            new GameStartedMessage
            {
                PlayerViewOfGame = _game.GetStateFor(c.Value.PlayerData.PlayerId)
            }, cancellationToken))
        );

        var currentPlayerId = _game.CurrentPlayer.Id;

        await _channels[currentPlayerId].SendAsync(new ItsYourTurnMessage
        {
            PlayerViewOfGame = _game.GetStateFor(currentPlayerId)
        }, cancellationToken);
    }

    private CommandResult CreateGame()
    {
        if (_game != null)
        {
            return new FailureResult("Game already started");
        }
        var players = _channels.Select(c => new CrazyEightsPlayer
        {
            Id = c.Value.PlayerData.PlayerId,
            Name = c.Value.PlayerData.Name
        }).ToArray();
        _game = new CrazyEightsGame(Deck.Standard.Shuffle(), players);
        return new SuccessResult();
    }
}