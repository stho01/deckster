package no.forse.decksterlib


import kotlinx.coroutines.suspendCancellableCoroutine
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.authentication.UserModel
import no.forse.decksterlib.communication.DecksterApi
import no.forse.decksterlib.communication.DecksterGameInitiater
import no.forse.decksterlib.communication.DecksterWebSocketListener
import no.forse.decksterlib.communication.MessageSerializer
import no.forse.decksterlib.communication.WebSocketConnection
import okhttp3.OkHttpClient
import okhttp3.Request
import retrofit2.Retrofit
import retrofit2.converter.jackson.JacksonConverterFactory
import java.io.IOException

class DecksterServer(
    private val hostAddress: String,
) {
    val okHttpClient = OkHttpClient.Builder().build()
    val hostBaseUrl = "http://$hostAddress"

    private val api = Retrofit.Builder()
        .baseUrl(hostBaseUrl)
        .client(okHttpClient)
        .addConverterFactory(JacksonConverterFactory.create(MessageSerializer.jackson))
        .build()
        .create(DecksterApi::class.java)



    fun getGameInstance(gameName: String, token: String): DecksterGameInitiater {
        return DecksterGameInitiater(this, gameName, token)
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

class LoginFailedException(cause: IOException) : Throwable(cause)