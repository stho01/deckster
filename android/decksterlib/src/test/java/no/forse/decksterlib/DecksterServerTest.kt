package no.forse.decksterlib

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.chatroom.ChatRoomClient
import org.junit.Test


class DecksterServerTest {

    //val token = "706ea1f74d6d4fdea33403b89293b580de32a74ed4174cc29d04f93b85448670"
    val gameId = "18fe30561ed741eba7ea62e5353c7d37"

    @Test
    fun testChatRoom2() = runBlocking {
        val targetServer = DecksterServer("192.168.1.233:13992")
        val client = ChatRoomClient(targetServer)
        client.login(LoginModel("frode5", "1234"))
        client.joinGame(gameId)
        client.chatAsync("Hei på deg!")
        delay(300)
    }

    @Test
    fun testChatRoom() = runBlocking {
        // todo: extension function to create "type", replace

        // Connects to the chat room specified by gameId with token and sends a "hi there" message
        //
        val lib = DecksterServer("localhost:13992")

        val chatGame = ChatRoomClient(lib)
        chatGame.login(LoginModel("frode5", "1234"))
        chatGame.joinGame(gameId)
        chatGame.chatAsync(message = "hi there " + (Math.random() * 1000).toInt())

        CoroutineScope(Dispatchers.Default).launch {
            chatGame.playerSaid!!.collect() {
                println (" --> ${it.sender}: ${it.message}")
            }
        }
        Thread.sleep(5000)
        Unit
    }
}