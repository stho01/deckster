import Foundation

protocol WebSocketClientDelegate: AnyObject {
    func webSocketClientDidReceiveMessage(_ message: String)
    func webSocketClientDidDisconnect(error: Error)
}
