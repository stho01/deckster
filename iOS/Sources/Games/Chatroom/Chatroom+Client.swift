import Foundation

public enum Chatroom {}

extension Chatroom {
    public class Client: GameClient<Action, ActionResponse, Notification> {
        public init(hostname: String, gameId: String, accessToken: String) throws {
            try super.init(
                hostname: hostname,
                gameName: "chatroom",
                gameId: gameId,
                accessToken: accessToken
            )
        }

        public func send(message: String) async throws {
            _ = try await sendAction(.send(message: message))
        }
    }
}
