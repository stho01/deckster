import Foundation

public enum Idiot {}

extension Idiot {
    public class Client: GameClient<Action, ActionResponse, Notification> {
        public init(hostname: String, gameId: String, accessToken: String) throws {
            try super.init(
                hostname: hostname,
                gameName: "idiot",
                gameId: gameId,
                accessToken: accessToken
            )
        }
    }
}
