using Deckster.Client.Games.Idiot;

namespace Deckster.Server.Games.Idiot;

public class IdiotProjection : GameProjection<IdiotGame>
{
    public static IdiotGame Create(IdiotGameCreatedEvent created)
    {
        var game = IdiotGame.Create(created);
        game.Deal();
        return game;
    }

    public override (IdiotGame game, object startEvent) Create(IGameHost host)
    {
        var createdEvent = new IdiotGameCreatedEvent
        {
            Players = host.GetPlayers(),
            Deck = Decks.Standard.KnuthShuffle(new Random().Next(0, int.MaxValue))
        };

        var game = Create(createdEvent);
        return (game, createdEvent);
    }
 
    public Task Apply(IamReadyRequest @event, IdiotGame game) => game.IamReady(@event);
    public Task Apply(SwapCardsRequest @event, IdiotGame game) => game.SwapCards(@event);
    public Task Apply(PutCardsFromHandRequest @event, IdiotGame game) => game.PutCardsFromHand(@event);
    public Task Apply(PutCardsFacingUpRequest @event, IdiotGame game) => game.PutCardsFacingUp(@event);
    public Task Apply(PutCardFacingDownRequest @event, IdiotGame game) => game.PutCardFacingDown(@event);
    public Task Apply(DrawCardsRequest @event, IdiotGame game) => game.DrawCards(@event);
    public Task Apply(PullInDiscardPileRequest @event, IdiotGame game) => game.PullInDiscardPile(@event);
    public Task Apply(PutChanceCardRequest @event, IdiotGame game) => game.PutChanceCard(@event);
}