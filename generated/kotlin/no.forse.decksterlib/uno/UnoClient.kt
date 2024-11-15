/**
 * Autogenerated by really, really eager small hamsters.
 *
 * Notifications (events) for this game:
 * GameStarted: GameStartedNotification
 * PlayerPutCard: PlayerPutCardNotification
 * PlayerPutWild: PlayerPutWildNotification
 * PlayerDrewCard: PlayerDrewCardNotification
 * PlayerPassed: PlayerPassedNotification
 * GameEnded: GameEndedNotification
 * ItsYourTurn: ItsYourTurnNotification
 * RoundStarted: RoundStartedNotification
 * RoundEnded: RoundEndedNotification
 *
*/
package no.forse.decksterlib.uno

interface UnoClient {
    suspend fun putCard(request: PutCardRequest): PlayerViewOfGame
    suspend fun putWild(request: PutWildRequest): PlayerViewOfGame
    suspend fun drawCard(request: DrawCardRequest): UnoCardResponse
    suspend fun pass(request: PassRequest): EmptyResponse
}
