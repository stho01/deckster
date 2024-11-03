package no.forse.decksterlib.game

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import no.forse.decksterlib.ConnectedDecksterGame
import no.forse.decksterlib.DecksterGame
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.model.protocol.DecksterNotification

abstract class GameClientBase(
    protected val decksterServer: DecksterServer,
    protected val gameName: String,
) {
    protected var game: DecksterGame? = null
    protected var joinedGame: ConnectedDecksterGame? = null
        private set
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
            // todo listen to BusinessActionResponse
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

    abstract fun onNotificationArrived(notif: DecksterNotification)
}