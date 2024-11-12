package no.forse.decksterlib.chatroom


data class GameState(
    /** Looks more like an ID at this point */
    val name: String = "",
    // "Waiting", etc
    val state: String = "",
    val players: List<Player> = emptyList()
)

data class Player(
    val name: String = "",
    val id: String = ""
)
