package no.forse.decksterandroid.chatroom

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import no.forse.decksterandroid.ChatRepository
import no.forse.decksterlib.chatroom.GameState
import kotlin.concurrent.timer

sealed interface ChatRoomUiState {
    data class ChatRoom(val games: List<GameState>) :
        ChatRoomUiState
}

class ChatRoomsViewModel(private val decksterRepository: ChatRepository) : ViewModel() {

    private val _uiState: MutableStateFlow<ChatRoomUiState> =
        MutableStateFlow(value = ChatRoomUiState.ChatRoom(emptyList()))
    val uiState: StateFlow<ChatRoomUiState> = _uiState.asStateFlow()


    fun join(id: String, onDone: () -> Unit) = viewModelScope.launch {
        decksterRepository.joinChat(id)
        onDone()
    }

    fun getGameList() = timer("GetGameList", period = 1000) {
        viewModelScope.launch {
            val games = decksterRepository.getGameList()
            _uiState.update { ChatRoomUiState.ChatRoom(games) }
        }
    }

    class Factory : ViewModelProvider.Factory {
        override fun <T : ViewModel> create(modelClass: Class<T>): T = ChatRoomsViewModel(
            ChatRepository
        ) as T
    }
}