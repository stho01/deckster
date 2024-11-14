using Deckster.Core.Games.Gabong;
using Deckster.Games;
using Deckster.Games.Gabong;

namespace Deckster.Server.Games.Gabong;

public class GabongProjection : GameProjection<GabongGame>
{
    public GabongGame Create(GabongGameCreatedEvent created)
    {
        var game = GabongGame.Instantiate(created);
        return game;
    }
    
    public override (GabongGame game, object startEvent) Create(IGameHost host)
    {
        var startEvent = new GabongGameCreatedEvent
        {
            Id = Guid.NewGuid(),
            Name = host.Name,
            Players = host.GetPlayers(),
            Deck = GabongDeck.Standard.KnuthShuffle(new Random().Next(0, int.MaxValue)),
        };

        var game = Create(startEvent);
        return (game, startEvent);
    }

    public Task Apply(PenalizePlayerForTakingTooLongRequest @event, GabongGame game) => game.PenalizeSlowPlayer(@event);
    public Task Apply(PenalizePlayerForTooManyCardsRequest @event, GabongGame game) => game.PenalizePlayerWithTooManyCards(@event);

    public Task Apply(PutCardRequest @event, GabongGame game) => game.PutCard(@event);
    public Task Apply(DrawCardRequest @event, GabongGame game) => game.DrawCard(@event);
    public Task Apply(PassRequest @event, GabongGame game) => game.Pass(@event);
    public Task Apply(PlayGabongRequest @event, GabongGame game) => game.PlayGabong(@event);
    public Task Apply(PlayBongaRequest @event, GabongGame game) => game.PlayBonga(@event);
}