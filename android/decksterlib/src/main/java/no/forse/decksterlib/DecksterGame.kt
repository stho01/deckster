package no.forse.decksterlib

import com.fasterxml.jackson.databind.DeserializationFeature
import com.fasterxml.jackson.databind.ObjectMapper
import com.fasterxml.jackson.module.kotlin.readValue
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.launch
import no.forse.decksterlib.common.DecksterRequest
import no.forse.decksterlib.common.DecksterResponse
import no.forse.decksterlib.handshake.ConnectFailureMessage
import no.forse.decksterlib.handshake.HelloSuccessMessage
import okhttp3.WebSocket
import kotlin.coroutines.Continuation
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException
import kotlin.coroutines.suspendCoroutine

class DecksterGame(
    val decksterServer: DecksterServer,
    val name: String,
    val token: String,
) {


    private val jackson = ObjectMapper()
        .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
    suspend fun join(gameId: String): ConnectedDecksterGame {
        val request = decksterServer.getRequest("$name/join/$gameId", token)
        val connection = decksterServer.connectWebSocket(request)
        return suspendCoroutine<ConnectedDecksterGame> { cont ->
            handleConnectionMessages(connection, cont)
        }
    }

    private fun handleConnectionMessages(connection: WebSocketConnection, cont: Continuation<ConnectedDecksterGame>) {
        CoroutineScope(Dispatchers.Default).launch {
            connection.messageFlow.collect { strMsg ->
                val type = getTypeOf(strMsg)
                type?.let {
                    // Deserialize full  object
                    val typedMessage = jackson.readValue(strMsg, it)
                    when (typedMessage) {
                        is HelloSuccessMessage -> joinConfirm(cont, connection.webSocket, typedMessage)
                        is ConnectFailureMessage -> {
                            println("ConnectFailure: ${typedMessage.errorMessage}")
                            cont.resumeWithException(ConnectFailureException(typedMessage.errorMessage))
                        }
                        else -> {
                            println("Type handling not implemented for $it")
                        }
                    }
                }
            }
        }
    }

    private fun getTypeOf(strMsg: String): Class<*>? {
        val shallowTypedMessage = jackson.readValue<DecksterResponse>(strMsg)
        val type: Class<*>? = when (shallowTypedMessage.type) {
            "Handshake.HelloSuccessMessage" -> HelloSuccessMessage::class.java
            "Handshake.ConnectFailureMessage" -> ConnectFailureMessage::class.java
            else -> {
                println("Unhandled type: ${shallowTypedMessage.type}")
                null
            }
        }
        return type
    }

    suspend fun joinConfirm(cont: Continuation<ConnectedDecksterGame>, actionSocket: WebSocket, helloSuccessMessage: HelloSuccessMessage) {
        val conId = helloSuccessMessage.connectionId
        val request = decksterServer.getRequest("$name/join/$conId/finish", token)
        println("Attempting finish join at : ${request.url}")
        val notificationConnection = decksterServer.connectWebSocket(request)
        handleConnectionMessages(notificationConnection, cont) //<- This doesnt work 100%. For handling ConnectionFailureMessage. Any ConnectionSuccess here
        cont.resume(
            ConnectedDecksterGame(this, actionSocket, notificationConnection.messageFlow)
        )
    }

    suspend fun send(socket: WebSocket, request: DecksterRequest): DecksterResponse? {
        val strMsg = jackson.writeValueAsString(request)
        println("Sending: $strMsg")
        socket.send(strMsg)
        return null
    }
}

/** A DecsterGame that is done handshaking connection.
 * actionSocket and notificationFlow is ready */
class ConnectedDecksterGame(
    val game: DecksterGame,
    val actionSocket: WebSocket,
    val notificationFlow: Flow<String>, // todo: Typesafe. E.g. ChatNotification
) {
    suspend fun send(message: DecksterRequest) {
        game.send(actionSocket, message)
    }
}

class ConnectFailureException(message: String) : RuntimeException(message)