package no.forse.decksterlib

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import no.forse.decksterlib.chatroom.SendChatMessage
import org.junit.Test


class DecksterServerTest {

    val token = "706ea1f74d6d4fdea33403b89293b580de32a74ed4174cc29d04f93b85448670"
    val gameId = "4d3516d6-8c79-49e4-9db2-3c21d40e3a54"

    @Test
    fun testChatRoom() = runBlocking {
        // Connects to the chat room specified by gameId with token and sends a "hi there" message
        val lib = DecksterServer("localhost:13992")
        val gameClient = lib.getGameInstance("chatroom", token)
        val game = gameClient.join(gameId)
        val msg = SendChatMessage("hi there " + (Math.random() * 1000).toInt())
        game.send(msg)
        CoroutineScope(Dispatchers.Default).launch {
            game.notificationFlow.collect {
                println ("Incoming: $it")
            }
        }
        Unit
    }
}