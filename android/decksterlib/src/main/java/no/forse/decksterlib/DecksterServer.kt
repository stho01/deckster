package no.forse.decksterlib


import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.suspendCancellableCoroutine
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.authentication.UserModel
import no.forse.decksterlib.communication.MessageSerializer
import okhttp3.Call
import okhttp3.Callback
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import okhttp3.Request
import okhttp3.RequestBody.Companion.toRequestBody
import okhttp3.Response
import okhttp3.WebSocket
import java.io.IOException
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException
import kotlin.coroutines.suspendCoroutine

class DecksterServer(
    private val hostAddress: String,
) {

    // todo: Probably private, encapsulate functions better
    val okHttpClient = OkHttpClient.Builder().build()
    private val serializer = MessageSerializer()

    fun getGameInstance(gameName: String, token: String): DecksterGame {
        return DecksterGame(this, gameName, token)
    }

    suspend fun login(credentials: LoginModel): UserModel {
        val authBody = serializer.serialize(credentials)
        val requestBody = authBody.toRequestBody("application/json".toMediaType())
        val request = Request.Builder()
            .method("POST", requestBody)
            .url("http://$hostAddress/login")
            .build()
        return suspendCancellableCoroutine<UserModel> { cont ->
            okHttpClient.newCall(request).enqueue(object : Callback {
                override fun onFailure(call: Call, e: IOException) {
                    cont.resumeWithException(LoginFailedException(e))
                }

                override fun onResponse(call: Call, response: Response) {
                    try {
                        val responseStr = response.body?.string()
                            ?: throw IOException("No body in response")
                        val user = serializer.deserialize(responseStr, UserModel::class.java)
                        cont.resume(user)
                    } catch (ex: Exception) {
                        cont.resumeWithException(ex)
                    }
                }
            })
        }
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

class LoginFailedException(cause: IOException) : Throwable(cause)