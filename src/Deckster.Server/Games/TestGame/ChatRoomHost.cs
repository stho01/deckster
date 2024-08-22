using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Deckster.Client.Common;
using Deckster.Client.Communication;
using Deckster.Client.Games.ChatRoom;
using Deckster.Client.Protocol;
using Deckster.Server.Communication;
using Deckster.Server.Games.CrazyEights;

namespace Deckster.Server.Games.TestGame;

public class ChatRoomHost : IGameHost
{
    public event EventHandler<CrazyEightsGameHost>? OnEnded;
    public Guid Id { get; } = Guid.NewGuid();

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
                await _players[player.PlayerId].ReplyAsync(new SuccessResponse());
                await BroadcastAsync(new ChatMessage
                {
                    Sender = player.Name,
                    Message = message.Message
                });
                return;
        }
        
        await _players[player.PlayerId].ReplyAsync(new FailureResponse($"Unknown request type {request.Type}"));
        
    }
    
    private Task BroadcastAsync(DecksterMessage message, CancellationToken cancellationToken = default)
    {
        return Task.WhenAll(_players.Values.Select(p => p.PostMessageAsync(message, cancellationToken).AsTask()));
    }

    public bool TryAddPlayer(IServerChannel channel, [MaybeNullWhen(true)] out string error)
    {
        if (!_players.TryAdd(channel.Player.PlayerId, channel))
        {
            Console.WriteLine($"Could not add player {channel.Player.Name}");
            error = "Could not add player";
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
        _players.Remove(channel.Player.PlayerId, out _);
        await BroadcastAsync(new ChatMessage
        {
            Sender = channel.Player.Name,
            Message = "Disconnected"
        });
    }

    public async Task CancelAsync(string reason)
    {
        foreach (var player in _players.Values.ToArray())
        {
            await player.DisconnectAsync(true, reason);
            player.Dispose();
        }
    }
}
