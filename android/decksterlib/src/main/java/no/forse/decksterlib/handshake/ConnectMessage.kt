package no.forse.decksterlib.handshake

import com.fasterxml.jackson.annotation.JsonSubTypes
import com.fasterxml.jackson.annotation.JsonTypeInfo

@JsonTypeInfo(
    use = JsonTypeInfo.Id.NAME,
    include = JsonTypeInfo.As.PROPERTY,
    property = "type")
@JsonSubTypes(
    JsonSubTypes.Type(value = HelloSuccessMessage::class, name = "Handshake.HelloSuccessMessage"),
    JsonSubTypes.Type(value = ConnectFailureMessage::class, name = "Handshake.ConnectFailureMessage"),
)
abstract class ConnectMessage

data class HelloSuccessMessage(
    // val playerData: PlayerData,
    var connectionId: String = ""
): ConnectMessage()

data class ConnectFailureMessage(var errorMessage: String = ""): ConnectMessage()
