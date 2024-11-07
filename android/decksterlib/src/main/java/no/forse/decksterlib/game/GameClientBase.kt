package no.forse.decksterlib.game

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.shareIn
import kotlinx.coroutines.launch
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.communication.ConnectedDecksterGame
import no.forse.decksterlib.communication.DecksterGameInitiater
import no.forse.decksterlib.model.protocol.DecksterNotification
import no.forse.decksterlib.model.protocol.DecksterRequest
import no.forse.decksterlib.model.protocol.DecksterResponse
import threadpoolScope

abstract class GameClientBase(
    protected val decksterServer: DecksterServer,
    protected val gameName: String,
) {
    protected var game: DecksterGameInitiater? = null
    protected var joinedGame: ConnectedDecksterGame? = null
        private set

    protected val joinedGameOrThrow: ConnectedDecksterGame
        get() = joinedGame ?: throw IllegalStateException("Game is not joined")

    protected var notifFlowJob: Job? = null

    suspend fun createGame() {
        if (game == null) throw IllegalStateException("You need to login first")
        // todo
    }

    suspend fun joinGame(gameId: String): ConnectedDecksterGame {
        val loggedInGame = game ?: throw IllegalStateException("You need to login first")
        return loggedInGame.join(gameId).also {
            println ("-------- LIZTN START")
            joinedGame = it
            listenToBusinessNotifications()
        }
    }

    private fun listenToBusinessNotifications() {
        notifFlowJob = CoroutineScope(Dispatchers.Default).launch {
            joinedGame!!.notificationFlow.collect { notif ->
                onNotificationArrived(notif)
            }
        }
    }

    fun leaveGame() {
        notifFlowJob?.cancel()
        joinedGame?.leave() ?: throw IllegalStateException("Not connected")
    }

    suspend fun login(credentials: LoginModel) {
        val userModel = decksterServer.login(credentials)
        val token = userModel.accessToken ?: throw IllegalStateException("UserModel without token")
        game = decksterServer.getGameInstance(gameName, token)
    }

    @Suppress("UNCHECKED_CAST")
    protected suspend fun <T> sendAndReceive(message: DecksterRequest): T where T: DecksterResponse {
        val g = joinedGame ?: throw IllegalStateException("You need to log in and join a game")
        // actionResponseFlow is a flow for action responses for this and all previous and futute requests.
        // statement below cuts off all previous, while "first()" gives us the one and only response we are looking for.
        val responseFlowForRequest = g.actionResponseFlow.shareIn(threadpoolScope, SharingStarted.Eagerly, 1)
        g.send(message)
        return responseFlowForRequest.first() as T
    }


    abstract fun onNotificationArrived(notif: DecksterNotification)
}