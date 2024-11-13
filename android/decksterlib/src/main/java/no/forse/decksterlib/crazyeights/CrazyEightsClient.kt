package no.forse.decksterlib.crazyeights

import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.launch
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.game.GameClientBase
import no.forse.decksterlib.model.common.Card
import no.forse.decksterlib.model.common.EmptyResponse
import no.forse.decksterlib.model.common.Suit
import no.forse.decksterlib.model.crazyeights.*
import no.forse.decksterlib.model.protocol.DecksterNotification
import no.forse.decksterlib.protocol.getType
import threadpoolScope

/**
 * CrazyEights game.
 * Spec:
 * http://localhost:13992/crazyeights/metadata
 * http://localhost:13992/swagger/index.html
 */
class CrazyEightsClient(decksterServer: DecksterServer) :
    GameClientBase(decksterServer, "crazyeights") {

    override suspend fun onNotificationArrived(notif: DecksterNotification) {
        println("CrazyEightsClient ${joinedGame?.userUuid} onMessageArrived: $notif")
    }

    var currentState: PlayerViewOfGame? = null
        private set

    var gameStarted = CompletableDeferred<PlayerViewOfGame>()
        private set

    val yourTurnFlow: MutableSharedFlow<PlayerViewOfGame> = MutableSharedFlow(replay = 0, extraBufferCapacity = 1)

    private fun onGameStarted(notif: GameStartedNotification) {
        currentState = notif.playerViewOfGame
        gameStarted.complete(notif.playerViewOfGame)
    }

    val crazyEightsNotifications: Flow<DecksterNotification>?
        get() = joinedGame?.notificationFlow

    override fun onGameLeft() {
        currentState = null
        gameStarted = CompletableDeferred()
    }

    override fun onGameJoined() {
        threadpoolScope.launch {
            crazyEightsNotifications!!.collect { event ->
                println ("Crazy eight notif received, type: ${event.type}")
                when (event) {
                    is GameStartedNotification -> onGameStarted(event)
                    is ItsYourTurnNotification -> onYourTurn(event)
                }
            }
        }
    }

    private fun onYourTurn(event: ItsYourTurnNotification) {
        threadpoolScope.launch {
            yourTurnFlow.emit(event.playerViewOfGame)
        }
    }

    suspend fun passTurn(): EmptyResponse {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = PassRequest("", playerId)
        val typedMessage = PassRequest(passRequest.getType(), playerId)
        return sendAndReceive<EmptyResponse>(typedMessage)
    }

    suspend fun drawCard(): CardResponse {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = DrawCardRequest("", playerId)
        val typedMessage = DrawCardRequest(passRequest.getType(), playerId)
        return sendAndReceive<CardResponse>(typedMessage)
    }

    suspend fun putCard(card: Card): PlayerViewOfGame {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = PutCardRequest("", playerId, card)
        val typedMessage = PutCardRequest(passRequest.getType(), playerId, card)
        return sendAndReceive<PlayerViewOfGame>(typedMessage).also {
            this.currentState = it
        }
    }

    suspend fun putEight(card: Card, suit: Suit): PlayerViewOfGame {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = PutEightRequest("", playerId, card, suit)
        val typedMessage = PutEightRequest(passRequest.type, playerId, card, suit)
        return sendAndReceive<PlayerViewOfGame>(typedMessage).also {
            this.currentState = it
        }
    }
}