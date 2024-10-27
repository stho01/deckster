using Deckster.Client.Games.Common;
using Deckster.Client.Protocol;
using Deckster.Server.Communication;
using Deckster.Server.Data;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.CrazyEights;

namespace Deckster.Server.Games;

public abstract class StandardGameHost<TGame> : GameHost where TGame : GameObject
{
    protected readonly GameProjection<TGame> Projection;
    protected readonly Locked<TGame> Game = new();
    private readonly IRepo _repo;
    protected IEventQueue<TGame>? Events;

    protected StandardGameHost(IRepo repo, GameProjection<TGame> projection, int? playerLimit) : base(playerLimit)
    {
        Projection = projection;
        _repo = repo;
    }

    public override async Task StartAsync()
    {
        var game = Game.Value;
        if (game != null)
        {
            return;
        }
        
        (game, var startEvent) = Projection.Create(this);
        game.SetCommunication(this);
        var events = _repo.StartEventQueue<TGame>(game.Id, startEvent);

        Game.Value = game;
        Events = events;
        
        await game.StartAsync();
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
                await channel.ReplyAsync(new FailureResponse("Game is not running"), JsonOptions);
                return;
            }
            
            if (!await Projection.HandleAsync(request, game))
            {
                await channel.ReplyAsync(new FailureResponse($"Unsupported request: '{request.GetType().Name}'"), JsonOptions);
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
}