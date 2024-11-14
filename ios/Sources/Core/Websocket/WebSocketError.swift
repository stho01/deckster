import Foundation

enum WebSocketError: Error {
    case couldNotEncodeMessage
    case notConnected
    case unexpectedMessageType
}
