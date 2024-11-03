import Foundation

protocol WebSocketClientProtocol {
    var delegate: WebSocketClientDelegate? { get set }
    func connect()
    func disconnect()
    func sendMessage(_ message: String)
}
