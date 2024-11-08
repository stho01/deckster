package no.forse.decksterlib

import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.shareIn
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withTimeout
import org.junit.Test
import threadpoolScope

class FlowTest {
    // This test demonstrate how "shareIn" can be used to "restart" a sharedflow at a given
    // point. In this case "newFlow" starts after emission of 1 of 2 which are old and we want to
    // ignore, and collection of the third element starts immedately and synchonusly at line 33.
    // It is then irellevant how long it takes for the collector to become active - it will
    // either collect value "3" already emitted, or wait for it to be emitted. In other words
    // it will both work with delay (0) and delay(300) on line 39.
    // Prinicple used in DecksterGameBase for request-response related to actionsocket
    @Test
    fun testShareIn() {
        val flow = MutableSharedFlow<Long>()
        threadpoolScope.launch {
            flow.emit(1)
            flow.emit(2)
            delay(200)
            flow.emit(3)
        }
        Thread.sleep(100)

        val newFLow = flow.shareIn(threadpoolScope, SharingStarted.Eagerly, 1)

        runBlocking {
            delay(100) // This can be both 100 or 0 - the test will still pass
            withTimeout(2000) {
                val res = newFLow.first()
                println(res)
            }
        }
    }
}