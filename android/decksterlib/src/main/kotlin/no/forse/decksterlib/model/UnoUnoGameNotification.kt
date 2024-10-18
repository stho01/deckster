/**
 *
 * Please note:
 * This class is auto generated by OpenAPI Generator (https://openapi-generator.tech).
 * Do not edit this file manually.
 *
 */

@file:Suppress(
    "ArrayInDataClass",
    "EnumEntryName",
    "RemoveRedundantQualifierName",
    "UnusedImport"
)

package no.forse.decksterlib.model

import com.fasterxml.jackson.annotation.JsonSubTypes
import com.fasterxml.jackson.annotation.JsonTypeInfo

/**
 * 
 *
 * @param type 
 */
@JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.PROPERTY, property = "type", visible = true)
@JsonSubTypes(
    JsonSubTypes.Type(value = UnoGameEndedNotification::class, name = "Uno.GameEndedNotification"),
    JsonSubTypes.Type(value = UnoGameStartedNotification::class, name = "Uno.GameStartedNotification"),
    JsonSubTypes.Type(value = UnoItsYourTurnNotification::class, name = "Uno.ItsYourTurnNotification"),
    JsonSubTypes.Type(value = UnoPlayerDrewCardNotification::class, name = "Uno.PlayerDrewCardNotification"),
    JsonSubTypes.Type(value = UnoPlayerPassedNotification::class, name = "Uno.PlayerPassedNotification"),
    JsonSubTypes.Type(value = UnoPlayerPutCardNotification::class, name = "Uno.PlayerPutCardNotification"),
    JsonSubTypes.Type(value = UnoPlayerPutWildNotification::class, name = "Uno.PlayerPutWildNotification")
)

interface UnoUnoGameNotification : ProtocolDecksterNotification {


}

