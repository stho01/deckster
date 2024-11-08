package no.forse.decksterandroid.login

import android.widget.Toast
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Text
import androidx.compose.material3.TextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp

@Composable
fun LoginScreen(
    viewModel: LoginViewModel,
    onLoginSuccess: () -> Unit
) {
    val loginState = viewModel.uiState.collectAsState().value

    when (loginState) {
        is LoginUiState.Initial, is LoginUiState.Error -> {
            Login(loginState, onLoginPressed = { serverIp, username, password ->
                viewModel.login(serverIp, username.trim(), password)
            })
        }

        LoginUiState.Loading -> {
            Column(
                modifier = Modifier.fillMaxSize(),
                verticalArrangement = Arrangement.Center,
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text("Logging in...")
                Spacer(modifier = Modifier.height(16.dp))
                CircularProgressIndicator()
            }
        }

        LoginUiState.Success -> {
            LaunchedEffect(key1 = Unit) {

                onLoginSuccess.invoke()
            }
        }
    }
}

@Composable
fun Login(
    loginUiState: LoginUiState,
    onLoginPressed: (String, String, String) -> Unit
) {
    var decksterServerIp by remember {
        mutableStateOf(
            (loginUiState as? LoginUiState.Initial)?.details?.serverIp ?: ""
        )
    }
    var username by remember {
        mutableStateOf(
            (loginUiState as? LoginUiState.Initial)?.details?.username ?: ""
        )
    }
    var password by remember {
        mutableStateOf(
            (loginUiState as? LoginUiState.Initial)?.details?.password ?: ""
        )
    }

    var context = LocalContext.current

    LaunchedEffect(key1 = loginUiState is LoginUiState.Error) {
        if (loginUiState is LoginUiState.Error) {
            Toast.makeText(context, "Login failed.", Toast.LENGTH_SHORT).show()
        }
    }

    Column(
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally,
        modifier = Modifier.fillMaxSize()
    ) {
        Text("Deckster login")
        Spacer(modifier = Modifier.height(16.dp))
        Row {
            TextField(label = {
                Text("Deckster Server IP address")
            }, value = decksterServerIp, onValueChange = {
                decksterServerIp = it
            })
        }
        Row {
            TextField(label = {
                Text("Username")
            }, value = username, onValueChange = {
                username = it
            })
        }
        Row {
            TextField(label = {
                Text("Password")
            }, value = password, onValueChange = {
                password = it
            })
        }

        Spacer(modifier = Modifier.height(16.dp))
        Button(onClick = { onLoginPressed(decksterServerIp, username, password) }) {
            Text(text = "Login")
        }
    }
}