package no.forse.decksterandroid.chatroom

import BaseScreen
import android.util.Log
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import no.forse.decksterandroid.shared.theme.Typography

@Composable
fun GameRoom(
    viewModel: ChatRoomsViewModel,
    onEnter: (String) -> Unit,
) {
    BaseScreen(topBarTitle = "Gaming Rooms") {
        LaunchedEffect(key1 = true) {
            Log.d("ChatRoom", "LaunchedEffect")
            viewModel.getGameList()
        }

        val chatRoomUiState = viewModel.uiState.collectAsState().value

        when (chatRoomUiState) {
            is ChatRoomUiState.ChatRoom -> {
                LazyColumn(
                    contentPadding = PaddingValues(16.dp), verticalArrangement =
                    Arrangement.spacedBy(16.dp)
                ) {
                    item { Text("Chat Rooms", style = Typography.titleMedium) }
                    items(chatRoomUiState.games) { game ->
                        Card {
                            Column(modifier = Modifier.padding(16.dp)) {
                                Text(
                                    "Chat room id: ${game.name}"
                                )
                                Spacer(modifier = Modifier.height(8.dp))
                                Text("state: ${game.state}")
                                Spacer(modifier = Modifier.height(8.dp))
                                Text("players: ${
                                    game.players.joinToString(", ") { it.name }
                                }")
                                Spacer(modifier = Modifier.height(8.dp))

                                Row {
                                    Button(onClick = {
                                        viewModel.join(game.name) {
                                            onEnter(game.name)
                                        }
                                    }) {
                                        Text("Enter")
                                    }
                                }
                            }
                        }
                    }
                    item {
                        Text(
                            "CrazyEights and more coming soon...",
                            style = Typography.titleMedium
                        )
                    }
                }

            }
        }
    }
}

