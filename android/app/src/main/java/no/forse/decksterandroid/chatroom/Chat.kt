package no.forse.decksterandroid.chatroom

import BaseScreen
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
import androidx.compose.ui.unit.dp

@Composable
fun Chat(id: String?, viewModel: ChatViewModel, onBackpressed: () -> Unit) {
    LaunchedEffect(key1 = true) {
        viewModel.getChat(id)
    }

    val chatState = viewModel.chatState.collectAsState().value
    var message by remember { mutableStateOf("") }
    val state = remember { LazyListState() }

    BaseScreen(topBarTitle = "Chat", onBackPressed = {
        viewModel.leave()
        onBackpressed.invoke()
    }) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp)
        ) {
            Text(text = "Chat with ${chatState.users.map { it.name }.joinToString(separator = ",")}")
            LazyColumn(reverseLayout = true, modifier = Modifier.weight(1f), state = state) {
                items(chatState.chats.reversed()) { chat ->
                    ChatMessageItem(chat.message, chat.sender)
                }
            }
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween
            ) {

                TextField(value = message, onValueChange = {
                    message = it
                })
                Button(onClick = {
                    viewModel.sendMessage(message)
                    message = ""
                }) {
                    Text("Send")
                }
            }
        }
    }
}

@Composable
fun ChatMessageItem(message: String, from: String) {
    Text("$from: $message")
}
