package no.forse.decksterlib.chatroom

import retrofit2.http.GET

interface ChatRoomApi {
    @GET("/chatroom/games")
    suspend fun getGames() : List<GameState>
}