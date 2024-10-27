using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Common;
using Deckster.Client.Games.CrazyEights;
using Deckster.Client.Games.Uno;
using Deckster.CrazyEights.SampleClient;
using Deckster.Server.Communication;
using Deckster.Server.Data;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.Common.Fakes;
using Deckster.Server.Games.Uno.Core;
using Deckster.Uno.SampleClient;

namespace Deckster.Server.Games.Uno;

public class UnoGameHost : StandardGameHost<UnoGame>
{
    public override string GameType => "Uno";
    public override GameState State => Game.Value?.State ?? GameState.Waiting;
    private readonly List<UnoPoorAi> _bots = [];

    public UnoGameHost(IRepo repo) : base(repo, new UnoProjection(), 4)
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
        var bot = new UnoPoorAi(new UnoClient(channel));
        _bots.Add(bot);
        return TryAddPlayer(channel, out error);
    }
}