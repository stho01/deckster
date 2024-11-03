package no.forse.decksterlib

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.mapNotNull
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import no.forse.decksterlib.communication.MessageSerializer
import no.forse.decksterlib.coroutines.safeResume
import no.forse.decksterlib.coroutines.safeResumeWithException
import no.forse.decksterlib.model.handshake.ConnectFailureMessage
import no.forse.decksterlib.model.handshake.ConnectSuccessMessage
import no.forse.decksterlib.model.handshake.HelloSuccessMessage
import no.forse.decksterlib.model.protocol.DecksterMessage
import no.forse.decksterlib.model.protocol.DecksterNotification
import no.forse.decksterlib.model.protocol.DecksterRequest
import no.forse.decksterlib.model.protocol.DecksterResponse
import okhttp3.WebSocket
import java.util.*
import kotlin.coroutines.Continuation

class DecksterGame(
    val decksterServer: DecksterServer,
    val name: String,
    val token: String,
) {
    private val serializer = MessageSerializer()
    private var connectedDecksterGame: ConnectedDecksterGame? = null // Set after handhake and join
    private var handshakeJob: Job? = null

    suspend fun join(gameId: String): ConnectedDecksterGame {
        val request = decksterServer.getRequest("$name/join/$gameId", token)
        val connection = decksterServer.connectWebSocket(request)
        return suspendCancellableCoroutine<ConnectedDecksterGame> { cont ->
            handleConnectionMessages(connection, cont)
        }
    }

    private fun handleConnectionMessages(connection: WebSocketConnection, cont: Continuation<ConnectedDecksterGame>) {
        handshakeJob = CoroutineScope(Dispatchers.Default).launch {
            connection.messageFlow.collect { strMsg ->
                onMessageReceived(strMsg, connection, cont)
            }
        }
    }

    private suspend fun onMessageReceived(strMsg: String, connection: WebSocketConnection, cont: Continuation<ConnectedDecksterGame>) {
        // todo må ha to continuation. HelloSuccess sjkjer først, men så kommer ConnectFailureMessage eller
        println("-- onMessageReceived Message received:\n$strMsg")
        val typedMessage = serializer.tryDeserialize(strMsg, DecksterMessage::class.java)
        var handled = true
        when (typedMessage) {
            is HelloSuccessMessage -> startJoinConfirm(connection.webSocket, typedMessage, cont)
            is ConnectFailureMessage -> cont.safeResumeWithException(ConnectFailureException(typedMessage.errorMessage ?: "?"))
            null -> { handled = false }
            else -> {
                println("Type handling not implemented for ${typedMessage.javaClass}")
                handled = false
            }
        }
        if (handled) handshakeJob?.cancel("Done")
    }

    private fun handleConnectionMessage(
        strMsg: String,
        cont: Continuation<ConnectedDecksterGame>,
        actionSocket: WebSocket,
        helloSuccessMessage: HelloSuccessMessage,
        notificationConnection: WebSocketConnection
    ) {
        println("-- handleNotifMessageReceived Message received:\n$strMsg")
        val typedMessage = serializer.tryDeserialize(strMsg, DecksterMessage::class.java)
        when (typedMessage) {
            is ConnectSuccessMessage -> {
                // set up notificationFlow,. prepare connectedDecksterGame obj and complete continuation
                val notificationFlow = notificationConnection.messageFlow.mapNotNull {
                    serializer.tryDeserialize(it, DecksterNotification::class.java)
                }
                ConnectedDecksterGame(
                    this,
                    helloSuccessMessage.player?.id,
                    actionSocket,
                    notificationConnection.webSocket,
                    notificationFlow
                ).let {
                    connectedDecksterGame = it
                    cont.safeResume(it)
                }
            }
            is ConnectFailureMessage -> {
                println("ConnectFailure: ${typedMessage.errorMessage}")
                cont.safeResumeWithException(ConnectFailureException(typedMessage.errorMessage ?: "?"))
            }
        }
    }

    suspend fun startJoinConfirm(actionSocket: WebSocket, helloSuccessMessage: HelloSuccessMessage, cont: Continuation<ConnectedDecksterGame>) {
        val conId = helloSuccessMessage.connectionId
        val request = decksterServer.getRequest("$name/join/$conId/finish", token)
        println("Attempting finish join at : ${request.url}")
        val notificationConnection = decksterServer.connectWebSocket(request)

        // todo do something with ID below. Needed for chat later
        helloSuccessMessage.player.id

        CoroutineScope(Dispatchers.Default).launch {
            notificationConnection.messageFlow.collect { strMsg ->
                handleConnectionMessage(
                    strMsg,
                    cont,
                    actionSocket,
                    helloSuccessMessage,
                    notificationConnection
                )
            }
        }
    }

    suspend fun send(socket: WebSocket, request: DecksterRequest): DecksterResponse? {
        val strMsg = serializer.serialize(request)
        println("Sending: $strMsg")
        socket.send(strMsg)
        return null
    }
}

/** A DecsterGame that is done handshaking connection.
 * actionSocket and notificationFlow is ready */
class ConnectedDecksterGame(
    val game: DecksterGame,
    val userUuid: UUID?,
    val actionSocket: WebSocket,
    val notificationSocket: WebSocket,
    val notificationFlow: Flow<DecksterNotification>,
) {
    suspend fun send(message: DecksterRequest): Unit {
        game.send(actionSocket, message)
    }

    fun leave() {
        notificationSocket.close(1000, "Client disconnected")
        actionSocket.close(1000, "Client disconnected")
    }

    override fun toString() = game.toString()
}

class ConnectFailureException(message: String) : RuntimeException(message)