package no.forse.decksterlib


import kotlinx.coroutines.flow.Flow
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.WebSocket
import kotlin.coroutines.suspendCoroutine

class DecksterServer(
    private val hostAddress: String,
) {

    // todo: Probably private, encapsulate functions better
    val okHttpClient = OkHttpClient.Builder().build()

    fun getGameInstance(gameName: String, token: String): DecksterGame {
        return DecksterGame(this, gameName, token)
    }

    fun getRequest(path: String, token: String): Request {
        return Request.Builder()
            .url("ws://$hostAddress/$path")
            .addHeader("Content-Type", "application/json")
            .addHeader("Authorization", "Bearer $token")
            .build()
    }

    suspend fun connectWebSocket(request: Request): WebSocketConnection {
        return suspendCoroutine<WebSocketConnection> { cont ->
            val listener = DecksterWebSocketListener(cont)
            okHttpClient.newWebSocket(request, listener)
        }
    }
}

class WebSocketConnection(
    val webSocket: WebSocket,
    val messageFlow: Flow<String>,
)