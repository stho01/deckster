using Deckster.Core.Games.Common;
using Deckster.Core.Protocol;

namespace Deckster.Client.Communication.Handshake;

public abstract class ConnectMessage : DecksterMessage;

public class HelloSuccessMessage : ConnectMessage
{
    public PlayerData Player { get; set; }
    public Guid ConnectionId { get; set; }
}

public class ConnectSuccessMessage : ConnectMessage;

public class ConnectFailureMessage : ConnectMessage
{
    public string ErrorMessage { get; init; }
}