package no.forse.decksterlib.common

import kotlinx.coroutines.flow.Flow
import okhttp3.WebSocket

class GameConnection(
    val webSocket: WebSocket,
    val messageFlow: Flow<String>
)