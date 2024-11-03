package no.forse.decksterlib

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.chatroom.ChatRoomClient
import org.junit.Test


class DecksterServerTest {

    private fun prop(propName: String, defaultVal: String): String {
        val prop = System.getProperty(propName)
        if (prop.isNullOrBlank()) {
            println("WARN: Property '$propName' not set. Using default.")
            return defaultVal
        }
        return prop
    }

    @Test
    fun testChatRoom() = runBlocking {
        val lib = DecksterServer("localhost:13992")

        val chatGame = ChatRoomClient(lib)
        val gameId = prop("gameId", "57a05527e1fc4f79b98ade9450b3b1c7")
        val user = LoginModel(
            username = prop("userId", "defaultUser81"),
            password = prop("password", "1234"),
        )
        println("Attempting to join game as user '${user.username}', gameId '$gameId'")
        chatGame.login(user)

        println ("Joining game START...")
        chatGame.joinGame(gameId)
        println ("Joining game DONE...")
        chatGame.chatAsync(message = "hi there " + (Math.random() * 1000).toInt())

        CoroutineScope(Dispatchers.Default).launch {
            chatGame.playerSaid!!.collect() {
                println (" --> ${it.sender}: ${it.message}")
            }
        }
        Thread.sleep(3000)
        Unit
    }
}