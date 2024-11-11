using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Games.Idiot;
using Deckster.Client.Games.Uno;
using Deckster.Core.Games.Common;
using Deckster.Games.Idiot;
using Deckster.Idiot.SampleClient;
using Deckster.Server.Data;
using Deckster.Server.Games.Common.Fakes;

namespace Deckster.Server.Games.Idiot;

public class IdiotGameHost : StandardGameHost<IdiotGame>
{
    public override string GameType => "Idiot";
    private readonly List<IdiotPoorAi> _bots = [];
    
    public IdiotGameHost(IRepo repo, ILoggerFactory loggerFactory) : base(repo, loggerFactory, new IdiotProjection(), 4)
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
        var bot = new IdiotPoorAi(new IdiotClient(channel));
        _bots.Add(bot);
        return TryAddPlayer(channel, out error);
    }
}