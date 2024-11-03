package no.forse.decksterlib.chatroom

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.game.GameClientBase
import no.forse.decksterlib.model.chatroom.ChatNotification
import no.forse.decksterlib.model.chatroom.SendChatRequest
import no.forse.decksterlib.protocol.getType

class ChatRoomClient(
    decksterServer: DecksterServer
) : GameClientBase(decksterServer, "chatroom") {
    suspend fun chatAsync(message: String) {
        val room = joinedGame ?: throw IllegalStateException("You need to log in and join a game")
        val msg1 = SendChatRequest(message = message)
        val msg2 = msg1.copy(type = msg1.getType())
        room.send(msg2)
    }

    val playerSaid: Flow<ChatNotification>?
        get() = joinedGame?.notificationFlow?.map { it as ChatNotification }
}