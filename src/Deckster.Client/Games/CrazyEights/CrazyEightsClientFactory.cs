using Deckster.Client.Common;
using Deckster.Client.Communication;

namespace Deckster.Client.Games.CrazyEights;

public static class CrazyEightsClientFactory
{
    public static async Task<CrazyEightsClient> ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        var communicator = await DecksterClient.ConnectAsync(uri, cancellationToken);
        var client = new CrazyEightsClient(communicator);
        if (uri.AbsolutePath.EndsWith("practice"))
        {
            await communicator.SendAsync(new StartCommand(), cancellationToken);
            var response = await communicator.ReceiveAsync<CommandResult>(cancellationToken);
            if (response is FailureResult r)
            {
                throw new Exception(r.Message);
            }
        }
        return client;
    }
}