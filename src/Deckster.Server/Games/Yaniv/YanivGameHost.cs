using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Games.Yaniv;
using Deckster.Games;
using Deckster.Games.Collections;
using Deckster.Games.Yaniv;
using Deckster.Server.Communication;
using Deckster.Server.Data;

namespace Deckster.Server.Games.Yaniv;

public class YanivGameHost : StandardGameHost<YanivGame>
{
    public YanivGameHost(IRepo repo) : base(repo, new YanivProjection(), 5)
    {
    }

    public override string GameType => "Yaniv";
    
    protected override void ChannelDisconnected(IServerChannel channel)
    {
        throw new NotImplementedException();
    }

    public override bool TryAddBot([MaybeNullWhen(true)] out string error)
    {
        error = "bots not supported";
        return false;
    }
}

public class YanivProjection : GameProjection<YanivGame>
{
    public override (YanivGame game, object startEvent) Create(IGameHost host)
    {
        var createdEvent = new YanivGameCreatedEvent
        {
            Deck = Decks.Standard().PushRange(Decks.Jokers(2)).KnuthShuffle(new Random().Next(0, int.MaxValue)),
            Players = host.GetPlayers()
        };
        var game = YanivGame.Create(createdEvent);
        game.Deal();
        return (game, createdEvent);
    }

    public Task Apply(PutCardsRequest @event, YanivGame game) => game.PutCards(@event);
    public Task Apply(CallYanivRequest @event, YanivGame game) => game.CallYaniv(@event);
}