using Deckster.Client;
using Deckster.Client.Games.ChatRoom;
using Deckster.Client.Serialization;

namespace Deckster.ChatRoom.SampleClient;

class Program
{
    public static async Task<int> Main(string[] argz)
    {
        try
        {
            using var cts = new CancellationTokenSource();
            var deckster = new DecksterClient("http://localhost:13992", "abc123");
            await using var chatRoom = await deckster.ChatRoom.CreateAndJoinAsync("my-room", cts.Token);
            
            Console.WriteLine("Connected");
            chatRoom.OnMessage += m => Console.WriteLine($"Got message {m.Pretty()}");
            
            Console.CancelKeyPress += (s, e) =>
            {
                chatRoom.Dispose();
                cts.Cancel();
            };
          
            chatRoom.OnDisconnected += s =>
            {
                Console.WriteLine($"Disconnected: '{s}'");
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
                        Console.WriteLine(response.Pretty() ?? "null");
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
}