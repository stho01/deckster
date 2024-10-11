using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Games.ChatRoom;
using Deckster.Client.Protocol;
using Deckster.Client.Serialization;
using Deckster.Server.Communication;
using Deckster.Server.Games.Common;
using Deckster.Server.Games.CrazyEights;

namespace Deckster.Server.Games.TestGame;

public class ChatRoomHost : IGameHost
{
    public event EventHandler<CrazyEightsGameHost>? OnEnded;
    public string GameType => "ChatRoom";
    public GameState State => GameState.Running;
    public string Name { get; init; }

    private readonly ConcurrentDictionary<Guid, IServerChannel> _players = new();
    
    public Task Start()
    {
        return Task.CompletedTask;
    }

    private async void MessageReceived(PlayerData player, DecksterRequest request)
    {
        Console.WriteLine($"Received: {request.Pretty()}");

        switch (request)
        {
            case SendChatMessage message:
                await _players[player.Id].ReplyAsync(new SuccessResponse());
                await BroadcastAsync(new ChatNotification
                {
                    Sender = player.Name,
                    Message = message.Message
                });
                return;
        }
        
        await _players[player.Id].ReplyAsync(new FailureResponse($"Unknown request type {request.Type}"));
    }
    
    private Task BroadcastAsync(DecksterNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(notification, cancellationToken).AsTask()));
    }

    public bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (!_players.TryAdd(channel.Player.Id, channel))
        {
            Console.WriteLine($"Could not add player {channel.Player.Name}");
            error = "Player already exists";
            return false;
        }
        
        Console.WriteLine($"Added player {channel.Player.Name}");
        channel.Disconnected += ChannelDisconnected;

        channel.Received += MessageReceived;
        channel.Start(default);

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

    public async Task CancelAsync()
    {
        foreach (var player in _players.Values.ToArray())
        {
            await player.DisconnectAsync();
            player.Dispose();
        }
        _players.Clear();
    }

    public ICollection<PlayerData> GetPlayers()
    {
        return _players.Values.Select(c => c.Player).ToArray();
    }
}
