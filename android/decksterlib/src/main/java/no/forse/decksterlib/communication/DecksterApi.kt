package no.forse.decksterlib.communication

import no.forse.decksterlib.authentication.LoginModel
import no.forse.decksterlib.authentication.UserModel
import retrofit2.http.Body
import retrofit2.http.POST

interface DecksterApi {
    @POST("/login")
    suspend fun login(@Body credentials: LoginModel): UserModel
}