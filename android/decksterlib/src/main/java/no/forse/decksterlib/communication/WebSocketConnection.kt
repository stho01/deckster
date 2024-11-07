package no.forse.decksterlib.communication

import kotlinx.coroutines.flow.SharedFlow
import okhttp3.WebSocket

class WebSocketConnection(
    val webSocket: WebSocket,
    val messageFlowOneReplay: SharedFlow<String>,
    val messageFlowNoReplay: SharedFlow<String>,
)