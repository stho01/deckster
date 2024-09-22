using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Deckster.Client;
using Deckster.Client.Games.ChatRoom;
using Deckster.Client.Serialization;

namespace Deckster.CrazyEights.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        if (!TryGetUrl(argz, out _))
        {
            PrintUsage();
            return 0;
        }
        
        try
        {
            using var cts = new CancellationTokenSource();
            var deckster = new DecksterClient("http://localhost:13992", "abc123");
            await using var chatRoom = await deckster.ChatRoom.CreateAndJoinAsync(cts.Token);
            
            chatRoom.OnMessage += m => Console.WriteLine($"Got message {m.Pretty()}");
            
            Console.CancelKeyPress += (s, e) =>
            {
                chatRoom.Dispose();
                cts.Cancel();
            };
          
            chatRoom.OnDisconnected += s =>
            {
                Console.WriteLine($"Client disconnected: '{s}'");
                cts.Cancel();
            };
            
            while (!cts.IsCancellationRequested)
            {
                Console.WriteLine("Write message:");
                var message = await Console.In.ReadLineAsync(cts.Token);
                
                
                switch (message)
                {
                    case "quit":
                        await chatRoom.DisposeAsync();
                        return 0;
                    default:
                        Console.WriteLine($"Sending '{message}'");
                        var response = await chatRoom.SendAsync(new SendChatMessage
                        {
                            Message = message
                        }, cts.Token);
                
                        Console.WriteLine("Response:");
                        Console.WriteLine(response?.Pretty() ?? "null");
                        break;
                }
            }
            
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }

    private static bool TryGetUrl(string[] args, [MaybeNullWhen(false)] out Uri uri)
    {
        foreach (var a in args)
        {
            if (Uri.TryCreate(a, UriKind.Absolute, out uri))
            {
                return true;
            }
        }

        uri = new Uri($"ws://localhost:13992/crazyeights/join/{Guid.Empty}");
        return true;
    }

    private static void PrintUsage()
    {
        var usage = new StringBuilder()
            .AppendLine("Usage:")
            .AppendLine($"{Process.GetCurrentProcess().ProcessName} <uri>")
            .AppendLine($"e.g {Process.GetCurrentProcess().ProcessName} deckster://localhost:23023/123456");
        Console.WriteLine(usage);
    }
}