package no.forse.decksterandroid.login

import android.content.Context
import android.util.Log
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import no.forse.decksterandroid.ChatRepository

sealed interface LoginUiState {
    data class Initial(val details: LoginDetails) : LoginUiState
    object Loading : LoginUiState
    data class Error(val details: LoginDetails) : LoginUiState
    object Success : LoginUiState
}

class LoginViewModel(
    private val chatRepository: ChatRepository,
    private val appRepository: AppRepository
) : ViewModel() {

    private val loginDetails = appRepository.getLoginDetails()

    private val _uiState: MutableStateFlow<LoginUiState> =
        MutableStateFlow(value = LoginUiState.Initial(loginDetails))

    val uiState: StateFlow<LoginUiState> = _uiState.asStateFlow()

    fun login(serverIp: String, username: String, password: String) = viewModelScope.launch {

        _uiState.update { currentState ->
            LoginUiState.Loading
        }

        try {
            appRepository.saveLoginDetails(serverIp, username, password)
            chatRepository.login(serverIp, username, password)

            _uiState.update { currentState ->
                LoginUiState.Success
            }

        } catch (e: Exception) {
            Log.e("LoginViewModel", "Error logging in", e)
            _uiState.update { currentState ->
                LoginUiState.Error(loginDetails)
            }
            return@launch
        }
    }

    class Factory(private val context: Context) : ViewModelProvider.Factory {
        override fun <T : ViewModel> create(modelClass: Class<T>): T = LoginViewModel(
            ChatRepository,
            AppRepository(context)
        ) as T
    }
}

data class LoginDetails(
    val serverIp: String,
    val username: String,
    val password: String
)

class AppRepository(context: Context) {
    private val sharedPref = context.getSharedPreferences("login", Context.MODE_PRIVATE)

    fun saveLoginDetails(serverIp: String, username: String, password: String) {
        with(sharedPref.edit()) {
            putString("serverIp", serverIp)
            putString("username", username)
            putString("password", password)
            commit()
        }
    }

    fun getLoginDetails(): LoginDetails {
        return LoginDetails(
            serverIp = sharedPref.getString("serverIp", "192.168.0.10")!!,
            username = sharedPref.getString("username", "")!!,
            password = sharedPref.getString("password", "")!!
        )
    }
}

