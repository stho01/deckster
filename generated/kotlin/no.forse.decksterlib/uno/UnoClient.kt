package no.forse.decksterlib.uno

interface UnoClient {
  suspend fun putCard(card: UnoCard, cancellationToken: CancellationToken): UnoResponse
  suspend fun putWild(card: UnoCard, newColor: UnoColor, cancellationToken: CancellationToken): UnoResponse
  suspend fun drawCard(cancellationToken: CancellationToken): UnoCard
  suspend fun pass(cancellationToken: CancellationToken): UnoResponse
  suspend fun signalReadiness(cancellationToken: CancellationToken): Task
}
