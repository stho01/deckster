using System.Diagnostics.CodeAnalysis;
using Deckster.Games.Idiot;
using Deckster.Server.Communication;
using Deckster.Server.Data;

namespace Deckster.Server.Games.Idiot;

public class IdiotGameHost : StandardGameHost<IdiotGame>
{
    public override string GameType => "Idiot";
    
    public IdiotGameHost(IRepo repo) : base(repo, new IdiotProjection(), 4)
    {
    }
    
    protected override void ChannelDisconnected(IServerChannel channel)
    {
        
    }

    public override bool TryAddBot([MaybeNullWhen(true)] out string error)
    {
        error = "Bots not supported";
        return false;
    }
}