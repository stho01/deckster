package no.forse.decksterlib.chatroom

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.communication.MessageSerializer
import no.forse.decksterlib.game.GameClientBase
import no.forse.decksterlib.model.chatroom.ChatNotification
import no.forse.decksterlib.model.chatroom.SendChatRequest
import no.forse.decksterlib.model.protocol.DecksterNotification
import no.forse.decksterlib.protocol.getType
import retrofit2.Retrofit
import retrofit2.converter.jackson.JacksonConverterFactory

class ChatRoomClient(
    decksterServer: DecksterServer
) : GameClientBase(decksterServer, "chatroom") {

    private val api = Retrofit.Builder()
        .baseUrl(decksterServer.hostBaseUrl)
        .client(decksterServer.okHttpClient)
        .addConverterFactory(JacksonConverterFactory.create(MessageSerializer.jackson))
        .build()
        .create(ChatRoomApi::class.java)

    suspend fun chatAsync(message: String) {
        val room = joinedGame ?: throw IllegalStateException("You need to log in and join a game")
        val msg1 = SendChatRequest(message = message)
        val msg2 = msg1.copy(
            type = msg1.getType(),
            playerId = super.joinedGame?.userUuid
        )
        room.send(msg2)
        // todo: Await no.forse.decksterlib.model.common.EmptyResponse
    }

    suspend fun getGameList(): List<GameState> = api.getGames()

    val playerSaid: Flow<ChatNotification>?
        get() = joinedGame?.notificationFlow?.map { it as ChatNotification }

    override fun onNotificationArrived(notif: DecksterNotification) {
        println("ChatRoom onMessageArrived: $notif")
    }
}