package no.forse.decksterlib.chatroom

import no.forse.decksterlib.communication.DecksterNotification

data class ChatNotification(
    var type: String = "$NameSpace.ChatNotification",
    var message: String,
    var sender: String,
) : DecksterNotification()