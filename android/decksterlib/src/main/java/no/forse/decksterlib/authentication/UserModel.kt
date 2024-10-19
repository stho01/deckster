package no.forse.decksterlib.authentication


data class UserModel(
    var username: String? = "",
    var accessToken: String? = "",
)