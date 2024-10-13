using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.ChatRoom;
using Deckster.Client.Serialization;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;

namespace Deckster.Server.Games.ChatRoom;

public class ChatRoomHost : GameHost<ChatRequest, ChatResponse, ChatNotification>
{
    public override string GameType => "ChatRoom";
    public override GameState State => GameState.Running;

    private readonly ConcurrentDictionary<Guid, IServerChannel> _players = new();
    
    public override Task Start()
    {
        return Task.CompletedTask;
    }

    private async void MessageReceived(IServerChannel channel, ChatRequest request)
    {
        var player = channel.Player;
        Console.WriteLine($"Received: {request.Pretty()}");

        switch (request)
        {
            case SendChatMessage message:
                await _players[player.Id].ReplyAsync(new ChatResponse(), JsonOptions);
                await BroadcastAsync(new ChatNotification
                {
                    Sender = player.Name,
                    Message = message.Message
                });
                return;
        }
        
        await _players[player.Id].ReplyAsync(new FailureResponse($"Unknown request type {request.Type}"), JsonOptions);
    }
    
    private Task BroadcastAsync(ChatNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(notification, JsonOptions, cancellationToken).AsTask()));
    }

    public override bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (!_players.TryAdd(channel.Player.Id, channel))
        {
            Console.WriteLine($"Could not add player {channel.Player.Name}");
            error = "Player already exists";
            return false;
        }
        
        Console.WriteLine($"Added player {channel.Player.Name}");
        channel.Disconnected += ChannelDisconnected;
        
        channel.Start<ChatRequest>(MessageReceived, JsonOptions, default);

        error = default;
        return true;
    }

    private async void ChannelDisconnected(IServerChannel channel)
    {
        Console.WriteLine($"{channel.Player.Name} disconnected");
        _players.Remove(channel.Player.Id, out _);
        await BroadcastAsync(new ChatNotification
        {
            Sender = channel.Player.Name,
            Message = "Disconnected"
        });
    }

    public override async Task CancelAsync()
    {
        foreach (var player in _players.Values.ToArray())
        {
            await player.DisconnectAsync();
            player.Dispose();
        }
        _players.Clear();
    }

    public override ICollection<PlayerData> GetPlayers()
    {
        return _players.Values.Select(c => c.Player).ToArray();
    }
}
