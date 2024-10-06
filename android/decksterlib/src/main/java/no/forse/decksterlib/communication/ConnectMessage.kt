package no.forse.decksterlib.communication

import java.util.UUID

abstract class ConnectMessage {
}

data class HelloSuccessMessage(
    // val playerData: PlayerData,
    var connectionId: String = "",
): ConnectMessage()

data class ConnectFailureMessage(var errorMessage: String = ""): ConnectMessage()
