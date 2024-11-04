import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob


val threadpoolScope = CoroutineScope(Dispatchers.Default + SupervisorJob())