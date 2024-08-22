using System.Net.WebSockets;
using System.Text.Json;

namespace Deckster.Client.Communication.WebSockets;

public static class WebSocketExtensions
{
    public static ValueTask SendMessageAsync<T>(this WebSocket socket, T message, CancellationToken cancellationToken = default)
    {
        return socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(message, DecksterJson.Options),
            WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, cancellationToken);
    }

    public static async Task<T?> ReceiveMessageAsync<T>(this WebSocket socket, CancellationToken cancellationToken = default)
    {
        var buffer = new byte[512];
        var result = await socket.ReceiveAsync(buffer, cancellationToken);
        
        switch (result.MessageType)
        {
            case WebSocketMessageType.Text:
                return JsonSerializer.Deserialize<T>(new ReadOnlySpan<byte>(buffer, 0, result.Count), DecksterJson.Options);
            case WebSocketMessageType.Close:
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Server disconnected", default);
                throw new Exception($"WebSocket disconnected: {result.CloseStatus} '{result.CloseStatusDescription}'");
        }

        return default;
    }
}