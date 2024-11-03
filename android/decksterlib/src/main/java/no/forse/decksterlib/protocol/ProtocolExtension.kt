package no.forse.decksterlib.protocol

import no.forse.decksterlib.model.protocol.DecksterMessage

fun DecksterMessage.getType(): String = this.javaClass.simpleName.replace("XXX", ".")