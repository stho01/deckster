package no.forse.decksterandroid

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Scaffold
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import no.forse.decksterandroid.chatroom.Chat
import no.forse.decksterandroid.chatroom.ChatRoomsViewModel
import no.forse.decksterandroid.chatroom.ChatViewModel
import no.forse.decksterandroid.chatroom.GameRoom
import no.forse.decksterandroid.login.LoginScreen
import no.forse.decksterandroid.login.LoginViewModel
import no.forse.decksterandroid.shared.theme.DecksterAndroidTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            DecksterAndroid()
        }
    }
}

@Composable
fun DecksterAndroid() {
    DecksterAndroidTheme {
        val navController = rememberNavController()
        val context = LocalContext.current
        Scaffold { p ->
            NavHost(
                navController = navController,
                startDestination = "login",
                modifier = Modifier.padding(p)
            ) {
                composable("login") {
                    val loginViewModel = viewModel(
                        modelClass = LoginViewModel::class.java,
                        factory = LoginViewModel.Factory(context)
                    )
                    LoginScreen(
                        viewModel = loginViewModel,
                        onLoginSuccess = {
                            navController.navigate("gameRoom")
                        }
                    )
                }

                composable("gameRoom") {
                    val chatRoomViewModel = viewModel(
                        modelClass = ChatRoomsViewModel::class.java,
                        factory = ChatRoomsViewModel.Factory()
                    )
                    GameRoom(viewModel = chatRoomViewModel, onEnter = { id ->
                        navController.navigate(
                            "gameRoom/$id"
                        )
                    })
                }

                composable("gameRoom/{id}") { backStackEntry ->
                    val id = backStackEntry.arguments?.getString("id")
                    val chatViewModel = viewModel(
                        modelClass = ChatViewModel::class.java,
                        factory = ChatViewModel.Factory()
                    )
                    Chat(id, viewModel = chatViewModel, onBackpressed = {
                        navController.popBackStack()
                    })
                }
            }
        }
    }
}