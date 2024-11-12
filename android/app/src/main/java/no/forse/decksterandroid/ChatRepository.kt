package no.forse.decksterandroid

import android.util.Log
import kotlinx.coroutines.flow.Flow
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.chatroom.ChatRoomClient
import no.forse.decksterlib.chatroom.GameState
import no.forse.decksterlib.communication.ConnectedDecksterGame
import no.forse.decksterlib.model.chatroom.ChatNotification

object ChatRepository {
    private var connectedGame: ConnectedDecksterGame? = null
    private var chatGame: ChatRoomClient? = null

    suspend fun joinChat(id: String) {
        Log.d("DecksterRepository", "join")
        connectedGame = chatGame?.joinGame(id)
        Log.d("DecksterRepository", "joined $connectedGame")

    }

    suspend fun leaveChat() {
        chatGame?.leaveGame() // TODO: when pressing back
    }

    suspend fun login(serverIp: String, username: String, password: String) {
        Log.d("DecksterRepository", "login")
        val decksterServer = DecksterServer("$serverIp:13992")
        chatGame = ChatRoomClient(decksterServer)
        chatGame?.login(LoginModel(username, password))
    }


    suspend fun sendMessage(message: String) {
        Log.d("DecksterRepository", "sendMessage")
        chatGame?.chatAsync(message)
    }

    suspend fun getGameList(): List<GameState> {
        Log.d("DecksterRepository", "getGameList")
        return chatGame!!.getGameList()
    }

    fun getChats(): Flow<ChatNotification> = chatGame?.playerSaid!!
}