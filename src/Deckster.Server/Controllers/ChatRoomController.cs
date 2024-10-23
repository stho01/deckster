using Deckster.Client.Games.ChatRoom;
using Deckster.Server.Data;
using Deckster.Server.Games;
using Deckster.Server.Games.ChatRoom;
using Microsoft.AspNetCore.Mvc;

namespace Deckster.Server.Controllers;

[Route("chatroom")]
public class ChatRoomController(GameHostRegistry hostRegistry, IRepo repo)
    : GameController<ChatRoomClient, ChatRoomHost, Chat>(hostRegistry, repo);