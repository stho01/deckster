import Foundation

extension Chatroom {
    public enum Notification: Decodable {
        case message(Message)

        enum CodingKeys: String, CodingKey {
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

    enum Kind: String, Decodable {
        case message = "ChatRoom.ChatNotification"
    }
}

// MARK: - Models

extension Chatroom.Notification {
    public struct Message: Decodable {
        public let sender: String
        public let message: String
    }
}
