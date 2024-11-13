package no.forse.decksterlib.communication

import no.forse.decksterlib.model.protocol.DecksterResponse

fun DecksterResponse.throwOnError() {
    if (this.hasError) throw ResponseErrorException(error ?: "")
}