import Foundation

public enum HandshakeError: Error {
    case handshakeFailed(kind: String)
}

struct HandshakeResponse: Decodable {
    let connectionId: String
}

struct NotificationSocketHandshake: Decodable {
    private enum CodingKeys: String, CodingKey {
        case kind = "type"
    }

    init(from decoder: any Decoder) throws {
        // "{\"type\":\"Handshake.ConnectSuccessMessage\"}"
        let container = try decoder.container(keyedBy: CodingKeys.self)
        let kind = try container.decode(String.self, forKey: .kind)

        if kind != "Handshake.ConnectSuccessMessage" {
            throw HandshakeError.handshakeFailed(kind: kind)
        }
    }
}
