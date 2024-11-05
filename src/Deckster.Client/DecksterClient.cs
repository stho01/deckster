using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Deckster.Core;
using Deckster.Core.Authentication;
using Deckster.Core.Extensions;
using Deckster.Core.Serialization;

namespace Deckster.Client;

public class DecksterClient
{
    public Uri BaseUri { get; }
    public string Token { get; }

    public DecksterClient(string url, string token) : this(new Uri(url), token)
    {
        
    }
    
    public DecksterClient(Uri baseUri, string token)
    {
        BaseUri = baseUri;
        Token = token;
    }

    public static async Task<DecksterClient> LogInOrRegisterAsync(string url, string username, string password)
    {
        var baseUri = new Uri(url);
        using var request = new HttpRequestMessage(HttpMethod.Post, baseUri.Append("login"))
            .WithJsonBody(new LoginModel
            {
                Username = username,
                Password = password
            });
        var client = new HttpClient();
        
        using var response = await client.SendAsync(request);
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
            {
                var model = await response.Content.ReadFromJsonAsync<UserModel>(DecksterJson.Options);
                if (model == null)
                {
                    throw new Exception("OMG CANT HAZ USERDAETA CUZ ITZ NULLZ");
                }
                return new DecksterClient(baseUri, model.AccessToken);
            }
            default:
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Authentication failed: {(int)response.StatusCode} {response.StatusCode}:\n{body}");
            }
        }
    }
}

public static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage WithJsonBody(this HttpRequestMessage request, object input)
    {
        var stream = new MemoryStream();
        JsonSerializer.Serialize(stream, input, DecksterJson.Options);
        stream.Position = 0;
        request.Content = new StreamContent(stream)
        {
            Headers =
            {
                ContentType = MediaTypeHeaderValue.Parse("application/json")
            }
        };
        return request;
    }
}