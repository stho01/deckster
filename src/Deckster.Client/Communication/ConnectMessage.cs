using Deckster.Client.Common;
using Deckster.Client.Protocol;

namespace Deckster.Client.Communication;

[JsonDerived<ConnectMessage>]
public abstract class ConnectMessage : IHaveDiscriminator
{
    public string Type => GetType().Name;
}

public class HelloSuccessMessage : ConnectMessage
{
    public PlayerData Player { get; set; }
    public Guid ConnectionId { get; set; }
}

public class ConnectSuccessMessage : ConnectMessage
{
    
}

public class ConnectFailureMessage : ConnectMessage
{
    public string ErrorMessage { get; init; }
}