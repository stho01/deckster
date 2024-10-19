package no.forse.decksterlib


import no.forse.decksterlib.communication.MessageSerializer
import no.forse.decksterlib.handshake.ConnectFailureMessage
import no.forse.decksterlib.handshake.ConnectMessage
import no.forse.decksterlib.handshake.HelloSuccessMessage
import no.forse.decksterlib.model.ChatRoomXXXChatNotification
import org.junit.Assert.assertEquals
import org.junit.Test

class SerializationTest {
    @Test
    fun chatRoomMessageTest() {
        val json = """
            {
              "type": "ChatRoom.ChatNotification",
              "message": "hi", 
              "sender": "Rolf"
            }  
        """.trimIndent()

        val target = MessageSerializer()
        val result = target.deserializeNotification(json)
        assertEquals(result!!.javaClass, ChatRoomXXXChatNotification::class.java)
        val notif = result as ChatRoomXXXChatNotification
        assertEquals("hi", notif.message)
        assertEquals("Rolf", notif.sender)
    }

    @Test
    fun connectFailure() {
        val json = """
            {
              "type": "Handshake.ConnectFailureMessage",
              "errorMessage": "oops"
            }  
        """.trimIndent()

        val target = MessageSerializer()
        val result = target.tryDeserialize(json, ConnectMessage::class.java)
        assertEquals(result!!.javaClass, ConnectFailureMessage::class.java)
        val failure = result as ConnectFailureMessage
        assertEquals("oops", failure.errorMessage)
    }

    @Test
    fun helloSuccess() {
        val json = """
            {
              "type": "Handshake.HelloSuccessMessage",
              "connectionId": "1"
            }  
        """.trimIndent()

        val target = MessageSerializer()
        val result = target.tryDeserialize(json, ConnectMessage::class.java)
        assertEquals(result!!.javaClass, HelloSuccessMessage::class.java)
        val success = result as HelloSuccessMessage
        assertEquals("1", success.connectionId)
    }
}