package no.forse.decksterlib.communication

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.mapNotNull
import kotlinx.coroutines.launch
import kotlinx.coroutines.suspendCancellableCoroutine
import no.forse.decksterlib.DecksterServer
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
import threadpoolScope
import java.util.*
import kotlin.coroutines.Continuation

/**
 * Class responsible for joining and setting up comms for a game
 */
class DecksterGameInitiater(
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
        handshakeJob = threadpoolScope.launch {
            connection.messageFlowOneReplay.collect { strMsg ->
                handleHandshakePhase1(strMsg, connection, cont)
            }
        }
    }

    private suspend fun handleHandshakePhase1(strMsg: String, connection: WebSocketConnection, cont: Continuation<ConnectedDecksterGame>) {
        println("-- handleHandshakePhase1 Message received:\n$strMsg")
        val typedMessage = serializer.tryDeserialize(strMsg, DecksterMessage::class.java)
        var handled = true
        when (typedMessage) {
            is HelloSuccessMessage -> startJoinConfirm(connection, typedMessage, cont)
            is ConnectFailureMessage -> cont.safeResumeWithException(
                ConnectFailureException(typedMessage.errorMessage ?: "?")
            )
            null -> { /* Error logged in serializer */ }
            else -> {
                println("Type handling not implemented for ${typedMessage.javaClass}")
                handled = false
            }
        }
        if (handled) handshakeJob?.cancel()
    }

    private fun handleHandshakePhase2(
        strMsg: String,
        cont: Continuation<ConnectedDecksterGame>,
        actionConnection: WebSocketConnection,
        helloSuccessMessage: HelloSuccessMessage,
        notificationConnection: WebSocketConnection
    ) {
        println("-- handleNotifMessageReceived Message received:\n$strMsg")
        val typedMessage = serializer.tryDeserialize(strMsg, DecksterMessage::class.java)
        when (typedMessage) {
            is ConnectSuccessMessage -> {
                // set up notificationFlow,. prepare connectedDecksterGame obj and complete continuation
                val notificationFlow = notificationConnection.messageFlowNoReplay.mapNotNull {
                    serializer.tryDeserialize(it, DecksterNotification::class.java)
                }
                val actionResponseFlow = actionConnection.messageFlowNoReplay.mapNotNull {
                    serializer.tryDeserialize(it, DecksterResponse::class.java)
                }

                ConnectedDecksterGame(
                    this,
                    helloSuccessMessage.player.id ?: throw IllegalArgumentException("No UUID supplied in player response"),
                    actionConnection,
                    actionResponseFlow,
                    notificationConnection,
                    notificationFlow,
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

    suspend fun startJoinConfirm(actionConn: WebSocketConnection, helloSuccessMessage: HelloSuccessMessage, cont: Continuation<ConnectedDecksterGame>) {
        val conId = helloSuccessMessage.connectionId
        val request = decksterServer.getRequest("$name/join/$conId/finish", token)
        println("Attempting finish join at : ${request.url}")
        val notificationConnection = decksterServer.connectWebSocket(request)

        CoroutineScope(Dispatchers.Default).launch {
            notificationConnection.messageFlowOneReplay.collect { strMsg ->
                handleHandshakePhase2(
                    strMsg,
                    cont,
                    actionConn,
                    helloSuccessMessage,
                    notificationConnection
                )
            }
        }
    }

    fun send(socket: WebSocket, request: DecksterRequest): DecksterResponse? {
        val strMsg = serializer.serialize(request)
        println("Sending request: $strMsg")
        socket.send(strMsg)
        return null
    }
}

/** A DecsterGame that is done handshaking connection.
 * actionSocket and notificationFlow is ready */
class ConnectedDecksterGame(
    val game: DecksterGameInitiater,
    val userUuid: UUID,
    val actionConnection: WebSocketConnection,
    val actionResponseFlow: Flow<DecksterResponse>,
    val notificationConnection: WebSocketConnection,
    val notificationFlow: Flow<DecksterNotification>,
) {
    fun send(message: DecksterRequest) {
        game.send(actionConnection.webSocket, message)
    }

    fun leave() {
        notificationConnection.webSocket.close(1000, "Client disconnected")
        actionConnection.webSocket.close(1000, "Client disconnected")
    }

    override fun toString() = game.toString()
}

class ConnectFailureException(message: String) : RuntimeException(message)