using Deckster.Client.Games.Uno;
using Deckster.Server.Games.CrazyEights.Core;
using Deckster.Server.Games.Uno.Core;
using Marten.Events;

namespace Deckster.Server.Games.Uno;

public class UnoProjection : GameProjection<UnoGame>
{
    public override (UnoGame game, object startEvent) Create(IGameHost host)
    {
        var startEvent = new UnoGameCreatedEvent
        {
            Id = Guid.NewGuid(),
            Players = host.GetPlayers(),
            Deck = UnoDeck.Standard.KnuthShuffle(new Random().Next(0, int.MaxValue)),
        };

        return (UnoGame.Create(startEvent), startEvent);
    }

    public Task Apply(PutCardRequest @event, UnoGame game) => game.PutCard(@event.PlayerId, @event.Card);
    public Task Apply(PutWildRequest @event, UnoGame game) => game.PutWild(@event.PlayerId, @event.Card, @event.NewColor);
    public Task Apply(DrawCardRequest @event, UnoGame game) => game.DrawCard(@event.PlayerId);
    public Task Apply(PassRequest @event, UnoGame game) => game.Pass(@event.PlayerId);

}