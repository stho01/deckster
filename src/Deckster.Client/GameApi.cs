using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Deckster.Client.Communication.WebSockets;
using Deckster.Client.Serialization;

namespace Deckster.Client;

public class GameApi<TClient>
{
    private readonly Uri _baseUri;
    private readonly string _token;
    private readonly Func<WebSocketClientChannel, TClient> _createClient;
    
    public GameApi(Uri baseUri, string token, Func<WebSocketClientChannel, TClient> createClient)
    {
        _baseUri = baseUri;
        _token = token;
        _createClient = createClient;
    }

    public async Task<TClient> CreateAndJoinAsync(string gamename, CancellationToken cancellationToken = default)
    {
        var gameInfo = await CreateAsync(gamename, cancellationToken);
        var client = await JoinAsync(gameInfo.Id, cancellationToken);
        return client;
    }
    
    public async Task<TClient> JoinAsync(string gameName, CancellationToken cancellationToken = default)
    {
        var channel = await WebSocketClientChannel.ConnectAsync(_baseUri, gameName, _token, cancellationToken);
        return _createClient(channel);
    }

    public async Task<GameInfo> CreateAsync(string gamename, CancellationToken cancellationToken = default)
    {
        var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUri.Append($"create/{gamename}"))
        {
            Headers =
            {
                Accept = {MediaTypeWithQualityHeaderValue.Parse("application/json")},
                Authorization = AuthenticationHeaderValue.Parse($"Bearer {_token}")
            }
        };

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                {
                    var gameInfo = await response.Content.ReadFromJsonAsync<GameInfo>(DecksterJson.Options, cancellationToken: cancellationToken);
                    return gameInfo;
                }
                default:
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new Exception($"Could not ensure game:\n{request.Method} {request.RequestUri}\n{(int)response.StatusCode} ({response.StatusCode})\n{body}");
                }
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Could not {request.Method} {request.RequestUri}", e);
        }
    }
}