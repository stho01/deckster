import Foundation

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

// MARK: - Models

extension Chatroom.Action {
    struct MessageAction: Encodable {
        let type: String = "ChatRoom.SendChatRequest"
        let message: String
    }
}
