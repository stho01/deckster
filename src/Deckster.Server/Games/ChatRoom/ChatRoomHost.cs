using System.Diagnostics.CodeAnalysis;
using Deckster.Core.Games.ChatRoom;
using Deckster.Core.Protocol;
using Deckster.Core.Serialization;
using Deckster.Games;
using Deckster.Server.Communication;
using Deckster.Server.Data;

namespace Deckster.Server.Games.ChatRoom;

public class ChatRoomHost : GameHost
{
    public override string GameType => "ChatRoom";
    public override GameState State => GameState.Running;
    private readonly IRepo _repo;
    private readonly IEventQueue<Deckster.Games.ChatRoom.ChatRoom> _events;
    private readonly ChatRoomProjection _projection = new();
    private readonly Deckster.Games.ChatRoom.ChatRoom _chatRoom;

    public ChatRoomHost(IRepo repo) : base(null)
    {
        _repo = repo;
        (_chatRoom, var startEvent) = _projection.Create(this);
        _events = repo.StartEventQueue<Deckster.Games.ChatRoom.ChatRoom>(_chatRoom.Id, startEvent);
        _events.Append(startEvent);
    }

    public override Task StartAsync()
    {
        return Task.CompletedTask;
    }

    protected override async void RequestReceived(IServerChannel channel, DecksterRequest request)
    {
        Console.WriteLine($"Received: {request.Pretty()}");

        switch (request)
        {
            case SendChatRequest message:
                await _chatRoom.ChatAsync(message);
                _events.Append(message);
                await _events.FlushAsync();
                return;
        }
    }

    public override bool TryAddBot([MaybeNullWhen(true)] out string error)
    {
        error = "Bots not supported";
        return false;
    }

    protected override async void ChannelDisconnected(IServerChannel channel)
    {
        Console.WriteLine($"{channel.Player.Name} disconnected");
        Players.Remove(channel.Player.Id, out _);
        await NotifyAllAsync(new ChatNotification
        {
            Sender = channel.Player.Name,
            Message = "Disconnected"
        });
    }
}