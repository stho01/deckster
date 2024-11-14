using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Gabong;
using Deckster.Core.Games.Common;
using Deckster.Gabong.SampleClient;
using Deckster.Games.Gabong;
using Deckster.Server.Data;
using Deckster.Server.Games.Common.Fakes;

namespace Deckster.Server.Games.Gabong;

public class GabongGameHost : StandardGameHost<GabongGame>
{
    public override string GameType => "Gabong";
    private readonly List<GabongPoorAi> _bots = [];

    public GabongGameHost(IRepo repo, ILoggerFactory loggerFactory) : base(repo, loggerFactory, new GabongProjection(), 4)
    {
    }

    public override List<PlayerData> GetPlayers()
    {
        if (Game.Value == null)
        {
            return base.GetPlayers();
        }
        return Game.Value!.Players.Select(p => p.ToPlayerData()).ToList();
    }
    
    public override bool TryAddBot([MaybeNullWhen(true)] out string error)
    {
        var channel = new InMemoryChannel
        {
            Player = new PlayerData
            {
                Id = Guid.NewGuid(),
                Name = TestUserNames.Random()
            }
        };
        var bot = new GabongPoorAi(new GabongClient(channel));
        _bots.Add(bot);
        return TryAddPlayer(channel, out error);
    }
}