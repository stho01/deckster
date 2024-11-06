package no.forse.decksterandroid.chatroom

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListState
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.mapNotNull
import kotlinx.coroutines.launch
import no.forse.decksterandroid.ChatRepository
import no.forse.decksterlib.model.chatroom.ChatNotification

data class ChatState(val chats: List<ChatMessage>)

data class ChatMessage(val message: String, val sender: String)


class ChatViewModel(
    private val chatRepository: ChatRepository
) : ViewModel() {


    private val _chatState: MutableStateFlow<ChatState> = MutableStateFlow(
        value = ChatState(
            emptyList()
        )
    )
    val chatState: StateFlow<ChatState> = _chatState.asStateFlow()

    fun sendMessage(message: String) = viewModelScope.launch {
        chatRepository.sendMessage(message)
    }

    fun getChat() = viewModelScope.launch {
        val chats = chatRepository.getChats()
        chats.mapNotNull { it: ChatNotification ->
            val (sender, message) = (it.sender to it.message)
            if (sender != null && message != null) ChatMessage(message, sender) else null
        }.collect { chatMessage ->
            _chatState.value = ChatState(_chatState.value.chats + chatMessage)
        }
    }


    class Factory : ViewModelProvider.Factory {
        override fun <T : ViewModel> create(modelClass: Class<T>): T = ChatViewModel(
            ChatRepository
        ) as T
    }
}

@Composable
fun Chat(viewModel: ChatViewModel) {
    val chatState = viewModel.chatState.collectAsState().value
    LaunchedEffect(key1 = true) {
        viewModel.getChat()
    }

    var message by remember { mutableStateOf("") }
    var state = remember { LazyListState() }
    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
    ) {
        LazyColumn(reverseLayout = true, modifier = Modifier.weight(1f), state = state) {
            items(chatState.chats.reversed()) { chat ->
                ChatMessageItem(chat.message, chat.sender)
            }
        }
        Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {

            TextField(value = message, onValueChange = {
                message = it
            })
            Button(onClick = {
                viewModel.sendMessage(message)
            }) {
                Text("Send")
            }
        }
    }
}

@Composable
fun ChatMessageItem(message: String, from: String) {
    Text("$from: $message")
}

@Preview
@Composable
fun ChatPreview() {
    Chat(ChatViewModel(ChatRepository))
}