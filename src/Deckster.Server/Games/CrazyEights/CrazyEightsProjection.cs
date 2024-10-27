using Deckster.Client.Games.CrazyEights;
using Deckster.Server.Games.CrazyEights.Core;

namespace Deckster.Server.Games.CrazyEights;

public class CrazyEightsProjection : GameProjection<CrazyEightsGame>
{
    public CrazyEightsGame Create(CrazyEightsGameCreatedEvent created)
    {
        return CrazyEightsGame.Create(created);
    }
    
    public Task Apply(PutCardRequest @event, CrazyEightsGame game) => game.PutCard(@event.PlayerId, @event.Card);
    public Task Apply(PutEightRequest @event, CrazyEightsGame game) => game.PutEight(@event.PlayerId, @event.Card, @event.NewSuit);
    public Task Apply(DrawCardRequest @event, CrazyEightsGame game) => game.DrawCard(@event.PlayerId);
    public Task Apply(PassRequest @event, CrazyEightsGame game) => game.Pass(@event.PlayerId);
    
    public override (CrazyEightsGame game, object startEvent) Create(IGameHost host)
    {
        var startEvent = new CrazyEightsGameCreatedEvent
        {
            Id = Guid.NewGuid(),
            Players = host.GetPlayers(),
            Deck = Decks.Standard.KnuthShuffle(new Random().Next(0, int.MaxValue))
        };
        return (Create(startEvent), startEvent);
    }
}