import Foundation
import Combine

/// Core WebSocket Client
final class WebSocketClient: WebSocketClientProtocol {
    weak var delegate: WebSocketClientDelegate?
    private var webSocketTask: URLSessionWebSocketTask?
    private let url: URL
    private let accessToken: String
    private var isConnected = false

    init(url: URL, accessToken: String) {
        self.url = url
        self.accessToken = accessToken
    }

    func connect() {
        guard !isConnected else { return }
        let session = URLSession(configuration: .default)

        var request = URLRequest(url: url)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")

        webSocketTask = session.webSocketTask(with: request)
        webSocketTask?.resume()
        isConnected = true

        // Start receiving messages
        receiveMessage()
    }

    func disconnect() {
        webSocketTask?.cancel(with: .goingAway, reason: nil)
        isConnected = false
    }

    func sendMessage(_ message: String) {
        let wsMessage = URLSessionWebSocketTask.Message.string(message)
        webSocketTask?.send(wsMessage) { [weak self] error in
            if let error = error {
                self?.delegate?.webSocketClientDidDisconnect(error: error)
            }
        }
    }

    // MARK: - Private methods

    private func receiveMessage() {
        webSocketTask?.receive { [weak self] result in
            switch result {
            case .success(let message):
                switch message {
                case .string(let text):
                    self?.delegate?.webSocketClientDidReceiveMessage(text)
                case .data(let data):
                    self?.delegate?.webSocketClientDidReceiveMessage(String(data: data, encoding: .utf8) ?? "")
                default:
                    break
                }

                // Recursive call to keep listening
                self?.receiveMessage()
            case .failure(let error):
                self?.delegate?.webSocketClientDidDisconnect(error: error)
                self?.isConnected = false
            }
        }
    }
}
