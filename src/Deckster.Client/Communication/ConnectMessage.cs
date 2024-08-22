using Deckster.Client.Common;

namespace Deckster.Client.Communication;

public class ConnectMessage
{
    public PlayerData PlayerData { get; set; }
    public Guid ConnectionId { get; set; }
}