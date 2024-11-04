package no.forse.decksterlib


import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.suspendCancellableCoroutine
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.authentication.UserModel
import no.forse.decksterlib.communication.MessageSerializer
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.WebSocket
import retrofit2.Retrofit
import retrofit2.converter.jackson.JacksonConverterFactory
import java.io.IOException

class DecksterServer(
    private val hostAddress: String,
) {
    private val okHttpClient = OkHttpClient.Builder().build()
    private val api = Retrofit.Builder()
        .baseUrl("http://$hostAddress")
        .client(okHttpClient)
        .addConverterFactory(JacksonConverterFactory.create(MessageSerializer.jackson))
        .build()
        .create(DecksterApi::class.java)

    fun getGameInstance(gameName: String, token: String): DecksterGame {
        return DecksterGame(this, gameName, token)
    }

    suspend fun login(credentials: LoginModel): UserModel = api.login(credentials)

    fun getRequest(path: String, token: String): Request {
        return Request.Builder()
            .url("ws://$hostAddress/$path")
            .addHeader("Content-Type", "application/json")
            .addHeader("Authorization", "Bearer $token")
            .build()
    }

    suspend fun connectWebSocket(request: Request): WebSocketConnection {
        return suspendCancellableCoroutine<WebSocketConnection> { cont ->
            val listener = DecksterWebSocketListener(cont)
            okHttpClient.newWebSocket(request, listener)
        }
    }
}

class WebSocketConnection(
    val webSocket: WebSocket,
    val messageFlowOneReplay: Flow<String>,
    val messageFlowNoReplay: Flow<String>,
)

class LoginFailedException(cause: IOException) : Throwable(cause)