package no.forse.decksterlib.communication

import com.fasterxml.jackson.databind.DeserializationFeature
import com.fasterxml.jackson.databind.ObjectMapper
import com.fasterxml.jackson.module.kotlin.readValue
import no.forse.decksterlib.model.protocol.DecksterNotification

class MessageSerializer {
    companion object {
        val jackson = ObjectMapper()
            .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
    }

    fun <T> tryDeserialize(message: String, type: Class<T>): T? {
        return try {
            deserialize(message, type)
        } catch (ex: Exception) {
            println("Error deserializing: $ex. Data:\n$message")
            null
        }
    }

    fun <T> deserialize(message: String, type: Class<T>): T {
        return jackson.readValue<T>(message, type)
    }

    fun serialize(obj: Any): String = jackson.writeValueAsString(obj)

    fun deserializeNotification(message: String): DecksterNotification? {
        return try {
            jackson.readValue<DecksterNotification>(message)
        } catch (ex: Exception) {
            println("Error deserializing: $ex. Data:\n$message")
            null
        }
    }
}