package no.forse.decksterlib.communication

import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.authentication.UserModel
import no.forse.decksterlib.model.core.GameInfo
import retrofit2.http.Body
import retrofit2.http.POST
import retrofit2.http.Path

interface DecksterApi {
    @POST("/login")
    suspend fun login(@Body credentials: LoginModel): UserModel

    @POST("/{gameName}/create")
    suspend fun createGame(@Path("gameName") gameName: String): GameInfo

    @POST("{gameName}/games/{gameId}/start")
    suspend fun startGame(
        @Path("gameName") gameName: String,
        @Path("gameId") gameId: String
    ): GameInfo
}