package no.forse.decksterlib.chatroom

import no.forse.decksterlib.common.DecksterRequest

class SendChatMessage(val message: String) : DecksterRequest("$NameSpace.SendChatMessage")