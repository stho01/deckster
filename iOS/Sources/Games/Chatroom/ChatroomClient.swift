import Foundation

enum Chatroom {}

extension Chatroom {
    class Client: GameClient<Action, ActionResponse, Notification> {
        init(hostname: String, gameId: String, accessToken: String) throws {
            try super.init(
                hostname: hostname,
                gameName: "chatroom",
                gameId: gameId,
                accessToken: accessToken
            )
        }

        func send(message: String) async throws {
            _ = try await sendAndReceive(.send(message: message))
        }
    }
}


// MARK: - Action

extension Chatroom {
    enum Action: Encodable {
        case send(message: String)

        func encode(to encoder: any Encoder) throws {
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
    enum Notification: Decodable {
        case message(Message)

        private enum CodingKeys: String, CodingKey {
            case kind = "type"
        }

        init(from decoder: any Decoder) throws {
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

    struct Message: Decodable {
        let sender: String
        let message: String
    }
}

// MARK: - ActionResponse

extension Chatroom {
    // Empty response.
    struct ActionResponse: Decodable {}
}
