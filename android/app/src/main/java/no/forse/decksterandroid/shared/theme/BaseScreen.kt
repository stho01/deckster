
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.consumeWindowInsets
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.CenterAlignedTopAppBar
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.testTag
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import no.forse.decksterandroid.shared.theme.Typography

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun BaseScreen(
    modifier: Modifier = Modifier,
    topBarTitle: String,
    onBackPressed: (() -> Unit)? = null,
    content: @Composable (PaddingValues) -> Unit,
) {
    Scaffold(
        topBar = {
            CenterAlignedTopAppBar(
                windowInsets = WindowInsets(
                    top = 0.dp,
                    left = 0.dp,
                    right = 0.dp,
                    bottom = 0.dp
                ),
                title = {
                    Text(
                        topBarTitle,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        style = Typography.titleLarge
                    )
                },
                navigationIcon = {
                    if (onBackPressed != null) IconButton(onClick = { onBackPressed?.invoke() }) {
                        Icon(
                            Icons.AutoMirrored.Default.ArrowBack,
                            contentDescription = "Back",
                            modifier = Modifier.testTag("backButton")
                        )
                    }
                },

                scrollBehavior = TopAppBarDefaults.pinnedScrollBehavior(),
            )
        },
        modifier = modifier,
        content = { innerPadding ->
            Column(
                modifier = modifier
                    .consumeWindowInsets(innerPadding)
                    .padding(innerPadding),
            ) {
                content(innerPadding)
            }
        },
    )
}