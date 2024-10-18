package no.forse.decksterlib.chatroom

interface ChatRoomClient {
  suspend fun chat(request: ChatRequest, cancellationToken: CancellationToken): ChatResponse
}
