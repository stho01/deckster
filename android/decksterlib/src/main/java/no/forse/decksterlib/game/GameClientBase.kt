package no.forse.decksterlib.game

import no.forse.decksterlib.ConnectedDecksterGame
import no.forse.decksterlib.DecksterGame
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.authentication.LoginModel

abstract class GameClientBase(
    protected val decksterServer: DecksterServer,
    protected val gameName: String,
) {
    protected var game: DecksterGame? = null
    protected var joinedGame: ConnectedDecksterGame? = null
        private set

    suspend fun createGame() {
        if (game == null) throw IllegalStateException("You need to login first")
        // todo
    }

    suspend fun joinGame(gameId: String) {
        val loggedInGame = game ?: throw IllegalStateException("You need to login first")
        joinedGame = loggedInGame.join(gameId)
    }

    suspend fun leaveGame() {
        // todo
    }

    suspend fun login(credentials: LoginModel) {
        val userModel = decksterServer.login(credentials)
        val token = userModel.accessToken ?: throw IllegalStateException("UserModel without token")
        game = decksterServer.getGameInstance(gameName, token)
    }
}