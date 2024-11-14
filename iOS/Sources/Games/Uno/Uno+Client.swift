import Foundation

public enum Uno {}

extension Uno {
    public class Client: GameClient<Action, ActionResponse, Notification> {
        public init(hostname: String, gameId: String, accessToken: String) throws {
            try super.init(
                hostname: hostname,
                gameName: "uno",
                gameId: gameId,
                accessToken: accessToken
            )
        }
    }
}
