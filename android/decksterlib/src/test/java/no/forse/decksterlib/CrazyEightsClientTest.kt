package no.forse.decksterlib

import kotlinx.coroutines.delay
import kotlinx.coroutines.runBlocking
import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.crazyeights.CrazyEightsClient
import org.junit.Test

class CrazyEightsClientTest {

    @Test
    fun createStartGame(): Unit = runBlocking {
        val decksterServer = DecksterServer("localhost:13992")


        val crazyEightsClient = CrazyEightsClient(decksterServer)
        println("Logging in")
        crazyEightsClient.login(LoginModel("mkohm", "test"))
        println("Logged in")

        println("Creating game")
        val gameInfo = crazyEightsClient.createGame()
        println("Game created")

        println("Joining game ${gameInfo.id}")
        crazyEightsClient.joinGame(gameInfo.id!!)
        println("Game joined")


        crazyEightsClient.startGame(gameInfo.id)
    }

    @Test
    fun playGame(): Unit = runBlocking {
        val decksterServer = DecksterServer("localhost:13992")

        val crazyEightsClient1 = CrazyEightsClient(decksterServer)
        val crazyEightsClient2 = CrazyEightsClient(decksterServer)

        println("Logging in")
        crazyEightsClient1.login(LoginModel("mkohm", "test"))
        crazyEightsClient2.login(LoginModel("mkohm2", "test"))
        println("mkohm and mkohm2 logged in")

        val gameInfo = crazyEightsClient1.createGame()

        println("mkohm joining")
        crazyEightsClient1.joinGame(gameInfo.id!!)
        println("mkohm joined")

        println("mkohm2 joining")
        crazyEightsClient2.joinGame(gameInfo.id!!)
        println("mkohm2 joined")

        println("mkohm starting game")
        crazyEightsClient1.startGame(gameInfo.id)
        println("mkohm started game")

       crazyEightsClient1.crazyEightsNotifications?.collect {
            println("mkohm said: $it")
        }

        delay(10000)
        crazyEightsClient1.leaveGame()
        crazyEightsClient2.leaveGame()
    }


    @Test
    fun playExistingGame(): Unit = runBlocking {
        val decksterServer = DecksterServer("localhost:13992")

        val crazyEightsClient1 = CrazyEightsClient(decksterServer)

        println("Logging in")
        crazyEightsClient1.login(LoginModel("Roger", "1234"))
        println("Roger logged in")

        val gameInfo = crazyEightsClient1.createGame()
        crazyEightsClient1.joinGame("0b7041d4db60442cb21e6f98b11a9f48")
        println("Waiting for game to start...")
        val state = crazyEightsClient1.gameStarted.await()
        val myCards = state.cards
        println("MY CARDS:\n  " + myCards.joinToString("\n  "))

        delay(2000)
        crazyEightsClient1.putCard(myCards[0])  // May or may not be an allowed move

        delay(10000)
        crazyEightsClient1.leaveGame()
    }
}