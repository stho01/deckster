namespace Deckster.Server.Games.Idiot;

public class IdiotProjection : GameProjection<IdiotGame>
{
    public static IdiotGame Create(IdiotGameCreatedEvent created) => IdiotGame.Create(created);

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
    
    public Task Apply(PutCardsFromHandRequest @event, IdiotGame game) => game.PutCardsFromHand(@event);
    public Task Apply(DrawCardsRequest @event, IdiotGame game) => game.DrawCards(@event);
}