package no.forse.decksterlib.chatroom

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import no.forse.decksterlib.ConnectedDecksterGame
import no.forse.decksterlib.DecksterGame
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.model.ChatRoomXXXChatNotification
import no.forse.decksterlib.model.ChatRoomXXXSendChatMessage
import no.forse.decksterlib.protocol.getType

class ChatRoomClient(
    private val decksterServer: DecksterServer
) {
    private var game: DecksterGame? = null
    var connectedChatRoom: ConnectedDecksterGame? = null
        private set

    val incomingChatFlow: Flow<ChatRoomXXXChatNotification>?
        get() = connectedChatRoom?.notificationFlow?.map {
            it as ChatRoomXXXChatNotification
        }

    suspend fun login(credentials: LoginModel) {
        val userModel = decksterServer.login(credentials)
        val token = userModel.accessToken ?: throw IllegalStateException("UserModel without token")
        game = decksterServer.getGameInstance("chatroom", token)
    }

    suspend fun createGame() {
        if (game == null) throw IllegalStateException("You need to login first")
        // todo
    }

    suspend fun joinGame(gameId: String) {
        val loggedInGame = game ?: throw IllegalStateException("You need to login first")
        connectedChatRoom = loggedInGame.join(gameId)
    }

    suspend fun leaveGame() {
        // todo
    }

    suspend fun sendMessage(message: String) {
        val room = connectedChatRoom ?: throw IllegalStateException("You need to log in and join a game")
        val msg1 = ChatRoomXXXSendChatMessage(message)
        val msg2 = msg1.copy(type = msg1.getType())
        room.send(msg2)
    }
}