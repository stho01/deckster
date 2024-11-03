package no.forse.decksterlib

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.launch
import okhttp3.Response
import okhttp3.WebSocket
import okhttp3.WebSocketListener
import kotlin.coroutines.Continuation
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

class DecksterWebSocketListener(val cont: Continuation<WebSocketConnection>) : WebSocketListener() {
    val messageFlow = MutableSharedFlow<String>(replay = 1, extraBufferCapacity = 5)
    val messageScope = CoroutineScope(Dispatchers.Default)
    override fun onOpen(webSocket: WebSocket, response: Response) {
        println("On Open $webSocket, response; ${response.message}, isRedir: ${response.isRedirect}")
        cont.resume(WebSocketConnection(webSocket, messageFlow))
        super.onOpen(webSocket, response)
    }

    override fun onMessage(webSocket: WebSocket, text: String) {
        println("On messagestr: $text")
        messageScope.launch {
            messageFlow.emit(text)
        }
        super.onMessage(webSocket, text)
    }

    override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
        println ("Error in DecksterWebSocketListener.onFailure: $t")
        cont.resumeWithException(t)
        super.onFailure(webSocket, t, response)
    }

    override fun onClosed(webSocket: WebSocket, code: Int, reason: String) {
        println("On closed $webSocket, reason $reason")
        super.onClosed(webSocket, code, reason)
        messageScope.cancel("Socket closed")
    }
}