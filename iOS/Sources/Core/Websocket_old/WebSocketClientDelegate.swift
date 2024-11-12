import Foundation

protocol WebSocketClientDelegate: AnyObject {
    func webSocketClient(_ client: WebSocketClient, didReceiveMessage message: String)
    func webSocketClient(_ client: WebSocketClient, didDisconnectWithError error: Error)
}
