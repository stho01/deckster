using Deckster.Client;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Communication.Handshake;
using Deckster.Server.Users;

namespace Deckster.Server.Infrastructure;

public class ConnectionContext
{
    public IDecksterChannel Channel { get; }
    public User User { get; }
    public IServiceProvider Services { get; }
    public ConnectRequest Request { get; }
    public ConnectResponse Response { get; }
    
    public ConnectionContext(
        IDecksterChannel channel,
        ConnectRequest request,
        User user,
        IServiceProvider services)
    {
        Channel = channel;
        Request = request;
        User = user;
        Services = services;
        
        Response = new ConnectResponse
        {
            StatusCode = 200,
            Description = "OK",
            PlayerData = new PlayerData
            {
                Name = user.Name,
                PlayerId = user.Id
            }
        };
    }

    public void Close()
    {
        Channel.Dispose();
    }
}