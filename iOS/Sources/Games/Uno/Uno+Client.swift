import Foundation

public enum Uno {}

extension Uno {
    public class Client: GameClient<Action, ActionResponse, Notification> {
        public init(hostname: String, gameId: String, accessToken: String) throws {
            try super.init(
                hostname: hostname,
                gameName: "chatroom",
                gameId: gameId,
                accessToken: accessToken
            )
        }
    }
}
