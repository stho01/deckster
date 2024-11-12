package no.forse.decksterandroid.chatroom

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.mapNotNull
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import no.forse.decksterandroid.ChatRepository
import no.forse.decksterlib.chatroom.Player
import no.forse.decksterlib.model.chatroom.ChatNotification

data class ChatState(val chats: List<ChatMessage>, val users: List<Player>)

data class ChatMessage(val message: String, val sender: String)


class ChatViewModel(
    private val chatRepository: ChatRepository
) : ViewModel() {

    private var players: List<Player> = emptyList()

    private val _chatState: MutableStateFlow<ChatState> = MutableStateFlow(
        value = ChatState(
            emptyList(), players
        )
    )
    val chatState: StateFlow<ChatState> = _chatState.asStateFlow()

    fun sendMessage(message: String) = viewModelScope.launch {
        ChatRepository.sendMessage(message)
    }

    fun getChat(chatId: String?) = viewModelScope.launch {
        val chats = ChatRepository.getChats()
        val initialGamelist = ChatRepository.getGameList()
        players = initialGamelist.find { it.name == chatId }?.players ?: emptyList()
        _chatState.update {
            ChatState(emptyList(), players)
        }

        chats.mapNotNull { it: ChatNotification ->
            val (sender, message) = (it.sender to it.message)
            val gameList =
                ChatRepository.getGameList() // Since the name of the sender is not included in the notification we fetch it using the gamelist
            players = gameList
                .find { it.name == chatId }?.players ?: emptyList()
            val senderName = players.find { it.id == sender }?.name
            if (senderName != null && message != null) ChatMessage(message, senderName) else null
        }.collect { chatMessage ->
            _chatState.value = ChatState(_chatState.value.chats + chatMessage, players)
        }
    }

    fun leave() = viewModelScope.launch {
        ChatRepository.leaveChat()
    }

    class Factory : ViewModelProvider.Factory {
        override fun <T : ViewModel> create(modelClass: Class<T>): T = ChatViewModel(
            ChatRepository
        ) as T
    }
}