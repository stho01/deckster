package no.forse.decksterlib.crazyeights

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import no.forse.decksterlib.DecksterServer
import no.forse.decksterlib.game.GameClientBase
import no.forse.decksterlib.model.common.Card
import no.forse.decksterlib.model.common.Suit
import no.forse.decksterlib.model.crazyeights.CardResponse
import no.forse.decksterlib.model.crazyeights.DrawCardRequest
import no.forse.decksterlib.model.crazyeights.PassRequest
import no.forse.decksterlib.model.crazyeights.PutCardRequest
import no.forse.decksterlib.model.crazyeights.PutEightRequest
import no.forse.decksterlib.model.protocol.DecksterNotification
import no.forse.decksterlib.protocol.getType

class CrazyEightsClient(decksterServer: DecksterServer) :
    GameClientBase(decksterServer, "crazyeights") {

    override suspend fun onNotificationArrived(notif: DecksterNotification) {
        println("CrazyEightsClient ${joinedGame?.userUuid} onMessageArrived: $notif")
    }

    val crazyEightsNotifications: Flow<DecksterNotification>?
        get() = joinedGame?.notificationFlow?.map {
            it.also {
                println("I wonder why no one ever calls me :(")
                println("CrazyEightsClient ${joinedGame?.userUuid} onMessageArrived: $it")
            }
        }


    suspend fun passTurn() {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = PassRequest("", playerId)
        val typedMessage = PassRequest(passRequest.getType(), playerId)
        sendAndReceive<CardResponse>(typedMessage)
    }

    suspend fun drawCard() {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = DrawCardRequest("", playerId)
        val typedMessage = DrawCardRequest(passRequest.getType(), playerId)
        sendAndReceive<CardResponse>(typedMessage)
    }

    suspend fun putCard(card: Card) {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = PutCardRequest("", playerId, card)
        val typedMessage = PutCardRequest(passRequest.getType(), playerId, card)
        sendAndReceive<CardResponse>(typedMessage)
    }

    suspend fun putEight(card: Card, suit: Suit) {
        val playerId = joinedGameOrThrow.userUuid
        val passRequest = PutEightRequest("", playerId, card, suit)
        val typedMessage = PutEightRequest(passRequest.type, playerId, card, suit)
        sendAndReceive<CardResponse>(typedMessage)
    }
}