package no.forse.decksterlib.protocol

import no.forse.decksterlib.model.ProtocolXXXDecksterMessage

fun ProtocolXXXDecksterMessage.getType(): String = this.javaClass.simpleName.replace("XXX", ".")