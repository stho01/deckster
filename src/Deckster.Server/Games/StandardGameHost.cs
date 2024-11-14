using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;
using Deckster.Games;
using Deckster.Server.Communication;
using Deckster.Server.Data;

namespace Deckster.Server.Games;

public abstract class StandardGameHost<TGame> : GameHost where TGame : GameObject
{
    protected readonly GameProjection<TGame> Projection;
    protected readonly Locked<TGame> Game = new();
    private readonly IRepo _repo;
    protected IEventQueue<TGame>? Events;
    protected readonly ILoggerFactory LoggerFactory;
    protected readonly ILogger Logger;

    private bool _hasStarted;
    public override GameState State
    {
        get
        {
            var game = Game.Value;
            if (game != null)
            {
                return game.State;
            }
            return _hasStarted ? GameState.Finished : GameState.Waiting;

        }
    }

    protected StandardGameHost(IRepo repo, ILoggerFactory loggerFactory, GameProjection<TGame> projection, int? playerLimit) : base(playerLimit)
    {
        Projection = projection;
        _repo = repo;
        LoggerFactory = loggerFactory;
        Logger = loggerFactory.CreateLogger(GetType());
    }
    
    protected override async void ChannelDisconnected(IServerChannel channel, DisconnectReason reason)
    {
        switch (reason)
        {
            case DisconnectReason.ClientDisconnected:
                if (State == GameState.Running)
                {
                    await EndAsync(Game.Value?.Id);
                }
                break;
        }
    }

    public override async Task StartAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            var game = Game.Value;
            if (game != null)
            {
                return;
            }

            if (!Players.Any())
            {
                return;
            }
        
            (game, var startEvent) = Projection.Create(this);
            game.WireUp(this);
            var events = _repo.StartEventQueue<TGame>(game.Id, startEvent);

            Game.Value = game;
            Events = events;
            _hasStarted = true;
        
            await game.StartAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public override async Task NotifySelfAsync(DecksterRequest request)
    {
        await _semaphore.WaitAsync();
        var game = Game.Value;
        var events = Events;
        try
        {
            if (game == null || game.State == GameState.Finished)
            {
                return;
            }
            
            if (!await Projection.HandleAsync(request, game))
            {
                return;
            }
            events?.Append(request);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    protected override async void RequestReceived(IServerChannel channel, DecksterRequest request)
    {
        await _semaphore.WaitAsync();
        var game = Game.Value;
        var events = Events;
        try
        {
            if (game == null || game.State == GameState.Finished)
            {
                await channel.ReplyAsync(new EmptyResponse("Game is not running"), JsonOptions);
                return;
            }
            
            if (!await Projection.HandleAsync(request, game))
            {
                await channel.ReplyAsync(new EmptyResponse($"Unsupported request: '{request.GetType().Name}'"), JsonOptions);
                return;
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
                if (Game.Value == null)
                {
                    return;
                }
                Game.Value = null;

                try
                {
                    if (events != null)
                    {
                        await events.FlushAsync();
                        await events.DisposeAsync();
                    }
                
                    await EndAsync(game.Id);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Could not flush events");
                }
                
            }
            finally
            {
                _endSemaphore.Release();
            }
        }
    }
}