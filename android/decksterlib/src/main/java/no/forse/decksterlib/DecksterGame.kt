package no.forse.decksterlib

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.mapNotNull
import kotlinx.coroutines.launch
import no.forse.decksterlib.communication.MessageSerializer
import no.forse.decksterlib.model.handshake.ConnectFailureMessage
import no.forse.decksterlib.model.handshake.HelloSuccessMessage
import no.forse.decksterlib.model.protocol.DecksterMessage
import no.forse.decksterlib.model.protocol.DecksterNotification
import no.forse.decksterlib.model.protocol.DecksterRequest
import no.forse.decksterlib.model.protocol.DecksterResponse
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
    private val serializer = MessageSerializer()

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
                onMessageReceived(strMsg, connection, cont)
            }
        }
    }

    private suspend fun onMessageReceived(strMsg: String, connection: WebSocketConnection, cont: Continuation<ConnectedDecksterGame>) {
        val typedMessage = serializer.tryDeserialize(strMsg, DecksterMessage::class.java)
        when (typedMessage) {
            is HelloSuccessMessage -> joinConfirm(cont, connection.webSocket, typedMessage)
            is ConnectFailureMessage -> {
                println("ConnectFailure: ${typedMessage.errorMessage}")
                cont.resumeWithException(ConnectFailureException(typedMessage.errorMessage ?: "No error messsage supplied"))
            }
            null -> { /* Error logged in serializer */ }
            else -> {
                println("Type handling not implemented for ${typedMessage.javaClass}")
            }
        }
    }

    suspend fun joinConfirm(cont: Continuation<ConnectedDecksterGame>, actionSocket: WebSocket, helloSuccessMessage: HelloSuccessMessage) {
        val conId = helloSuccessMessage.connectionId
        val request = decksterServer.getRequest("$name/join/$conId/finish", token)
        println("Attempting finish join at : ${request.url}")
        val notificationConnection = decksterServer.connectWebSocket(request)
        handleConnectionMessages(notificationConnection, cont) //<- This doesnt work 100%. For handling ConnectionFailureMessage. Any ConnectionSuccess here
        val notificationFlow = notificationConnection.messageFlow.mapNotNull {
            serializer.tryDeserialize(it, DecksterNotification::class.java)
        }
        cont.resume(
            ConnectedDecksterGame(this, actionSocket, notificationFlow)
        )
    }

    suspend fun send(socket: WebSocket, request: DecksterRequest): DecksterResponse? {
        val strMsg = serializer.serialize(request)
        println("Sending: $strMsg")
        socket.send(strMsg)
        // todo how to get response from a websocket?
        return null
    }
}

/** A DecsterGame that is done handshaking connection.
 * actionSocket and notificationFlow is ready */
class ConnectedDecksterGame(
    val game: DecksterGame,
    val actionSocket: WebSocket,
    val notificationFlow: Flow<DecksterNotification>,
) {
    suspend fun send(message: DecksterRequest): Unit {
        game.send(actionSocket, message)
    }
}

class ConnectFailureException(message: String) : RuntimeException(message)