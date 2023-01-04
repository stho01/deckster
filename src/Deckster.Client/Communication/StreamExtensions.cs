using System.Text;
using System.Text.Json;
using Deckster.Client.Logging;
using Microsoft.Extensions.Logging;

namespace Deckster.Client.Communication;

public static class StreamExtensions
{
    private static readonly ILogger Logger = Log.Factory.CreateLogger(nameof(StreamExtensions));
    
    public static async Task SendMessageAsync(this Stream stream, byte[] message, CancellationToken cancellationToken = default)
    {
        await stream.WriteAsync(message.Length.ToBytes(), cancellationToken);
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace("Writing '{message}'", Encoding.UTF8.GetString(message));    
        }
        await stream.WriteAsync(message, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    public static async Task<byte[]> ReceiveMessageAsync(this Stream stream, CancellationToken cancellationToken = default)
    {
        var length = await stream.ReadMessageLengthAsync(cancellationToken);
        return await stream.ReadMessageAsync(length, cancellationToken);
    }

    private static async ValueTask<byte[]> ReadMessageAsync(this Stream stream, int length, CancellationToken cancellationToken)
    {
        try
        {
            var message = new byte[length];
            await stream.ReadExactlyAsync(message, cancellationToken);
            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace("Receive {message}", Encoding.UTF8.GetString(message));    
            }
            return message;
        }
        catch
        {
            Logger.LogError("Could not read message of length {length}", length);
            throw;
        }
    }

    private static async ValueTask<int> ReadMessageLengthAsync(this Stream stream, CancellationToken cancellationToken)
    {
        var lengthBytes = new byte[4];
        await stream.ReadExactlyAsync(lengthBytes, cancellationToken);
        var length = lengthBytes.ToInt();
        return length;
    }

    public static Task SendJsonAsync<T>(this Stream stream, T message, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        JsonSerializer.Serialize(memoryStream, message, DecksterJson.Options);
        return stream.SendMessageAsync(memoryStream.ToArray(), cancellationToken);
    }
    
    public static async Task<T?> ReceiveJsonAsync<T>(this Stream stream, CancellationToken cancellationToken = default)
    {
        var bytes = await stream.ReceiveMessageAsync(cancellationToken);
        try
        {
            var value = JsonSerializer.Deserialize<T>(bytes, DecksterJson.Options);
            return value;
        }
        catch
        {
            Logger.LogError("Could not read '{message}'", Encoding.UTF8.GetString(bytes));
            throw;
        }
    }
}