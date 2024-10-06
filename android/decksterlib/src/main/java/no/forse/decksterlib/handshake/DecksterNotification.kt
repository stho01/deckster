package no.forse.decksterlib.handshake

import com.fasterxml.jackson.annotation.JsonSubTypes
import com.fasterxml.jackson.annotation.JsonTypeInfo
import no.forse.decksterlib.chatroom.ChatNotification

@JsonTypeInfo(
    use = JsonTypeInfo.Id.NAME,
    include = JsonTypeInfo.As.PROPERTY,
    property = "type")
@JsonSubTypes(
    JsonSubTypes.Type(value = ChatNotification::class, name = "ChatRoom.ChatNotification")
)
interface DecksterNotification