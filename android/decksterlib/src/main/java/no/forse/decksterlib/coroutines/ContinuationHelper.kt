package no.forse.decksterlib.coroutines

import kotlinx.coroutines.CancellableContinuation
import kotlin.coroutines.Continuation
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

// These functions are for better logging / debugging.

/** Does not throw if already resumed but logs a message instead */
fun <T> Continuation<T>.safeResume(value: T) {
    println("safeResume on $this, value: $value")
    when {
        this is CancellableContinuation && isActive -> resume(value)
        this is CancellableContinuation && !isActive -> println("resume completed. Can't resume again: $this")
        else -> throw Exception("Must use suspendCancellableCoroutine instead of suspendCoroutine")
    }

}

/** Logs more metadata when a resumed cont. is attempting to throw */
fun <T> Continuation<T>.safeResumeWithException(ex: Throwable) {
    println("safeResumeWithException on $this, ex: $ex")
    when {
        this is CancellableContinuation -> resumeWithException(ex)  // may throw
        else -> throw Exception("Must use suspendCancellableCoroutine instead of suspendCoroutine")
    }
}