package no.forse.decksterlib.handshake

abstract class ConnectMessage {
}

data class HelloSuccessMessage(
    // val playerData: PlayerData,
    var connectionId: String = "",
): ConnectMessage()

data class ConnectFailureMessage(var errorMessage: String = ""): ConnectMessage()
