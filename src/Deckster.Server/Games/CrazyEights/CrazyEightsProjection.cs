using Deckster.Core.Games.CrazyEights;
using Deckster.Games;
using Deckster.Games.CrazyEights;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsProjection : GameProjection<CrazyEightsGame>
{
    public CrazyEightsGame Create(CrazyEightsGameCreatedEvent created)
    {
        var game = CrazyEightsGame.Instantiate(created);
        return game;
    }
    
    public override (CrazyEightsGame game, object startEvent) Create(IGameHost host)
    {
        var startEvent = new CrazyEightsGameCreatedEvent
        {
            Id = Guid.NewGuid(),
            Name = host.Name,
            Players = host.GetPlayers(),
            Deck = Decks.Standard().KnuthShuffle(new Random().Next(0, int.MaxValue))
        };
        var game = Create(startEvent);
        return (game, startEvent);
    }
    
    public Task Apply(PutCardRequest @event, CrazyEightsGame game) => game.PutCard(@event);
    public Task Apply(PutEightRequest @event, CrazyEightsGame game) => game.PutEight(@event);
    public Task Apply(DrawCardRequest @event, CrazyEightsGame game) => game.DrawCard(@event);
    public Task Apply(PassRequest @event, CrazyEightsGame game) => game.Pass(@event);
}