using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Gabong;
using Deckster.Client.Games.Uno;
using Deckster.Core.Games.Common;
using Deckster.Gabong.SampleClient;
using Deckster.Games.Gabong;
using Deckster.Games.Uno;
using Deckster.Server.Communication;
using Deckster.Server.Data;
using Deckster.Server.Games.Common.Fakes;
using Deckster.Uno.SampleClient;

namespace Deckster.Server.Games.Gabong;

public class GabongGameHost : StandardGameHost<GabongGame>
{
    public override string GameType => "Gabong";
    private readonly List<GabongPoorAi> _bots = [];

    public GabongGameHost(IRepo repo) : base(repo, new GabongProjection(), 4)
    {
    }

    protected override void ChannelDisconnected(IServerChannel channel)
    {
        
    }

    public override bool TryAddBot([MaybeNullWhen(true)] out string error)
    {
        var channel = new InMemoryChannel
        {
            Player = new PlayerData
            {
                Id = Guid.NewGuid(),
                Name = TestNames.Random()
            }
        };
        var bot = new GabongPoorAi(new GabongClient(channel));
        _bots.Add(bot);
        return TryAddPlayer(channel, out error);
    }
}