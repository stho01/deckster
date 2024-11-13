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
            _ = try await sendAndReceive(.send(message: message))
        }
    }
}


// MARK: - Action

extension Chatroom {
    public enum Action: Encodable {
        case send(message: String)

        public func encode(to encoder: any Encoder) throws {
            switch self {
            case .send(let message):
                var container = encoder.singleValueContainer()
                try container.encode(MessageAction(message: message))
            }
        }
    }
}

extension Chatroom.Action {
    private struct MessageAction: Encodable {
        let type: String = "ChatRoom.SendChatRequest"
        let message: String
    }
}

// MARK: - Notification

extension Chatroom {
    public enum Notification: Decodable {
        case message(Message)

        private enum CodingKeys: String, CodingKey {
            case kind = "type"
        }

        public init(from decoder: any Decoder) throws {
            let container = try decoder.container(keyedBy: CodingKeys.self)
            let kind = try container.decode(Kind.self, forKey: .kind)

            switch kind {
            case .message:
                self = .message(try Message(from: decoder))
            }
        }
    }
}

extension Chatroom.Notification {
    enum Kind: String, Decodable {
        case message = "ChatRoom.ChatNotification"
    }

    public struct Message: Decodable {
        public let sender: String
        public let message: String
    }
}

// MARK: - ActionResponse

extension Chatroom {
    // Empty response.
    public struct ActionResponse: Decodable {}
}
