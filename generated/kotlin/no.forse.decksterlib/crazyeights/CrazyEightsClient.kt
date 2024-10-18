package no.forse.decksterlib.crazyeights

interface CrazyEightsClient {
  suspend fun putCard(card: Card, cancellationToken: CancellationToken): CrazyEightsResponse
  suspend fun putEight(card: Card, newSuit: Suit, cancellationToken: CancellationToken): CrazyEightsResponse
  suspend fun drawCard(cancellationToken: CancellationToken): Card
  suspend fun pass(cancellationToken: CancellationToken): CrazyEightsResponse
}
