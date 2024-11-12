import Foundation

final class WebSocketConnection {

    // MARK: - Internal properties

    let urlRequest: URLRequest

    var messageStream: AsyncThrowingStream<Data, Error> {
        AsyncThrowingStream { continuation in
            Task {
                await self.receiveMessages(continuation: continuation)
            }
        }
    }

    // MARK: - Private properties

    private var webSocketTask: URLSessionWebSocketTask?
    private let session: URLSession
    private let jsonEncoder = JSONEncoder()

    // MARK: - Init

    init(urlRequest: URLRequest,  session: URLSession = .shared) {
        self.urlRequest = urlRequest
        self.session = session
    }

    // MARK: - Internal methods

    func connect() {
        guard webSocketTask == nil else { return }
        webSocketTask = session.webSocketTask(with: urlRequest)
        webSocketTask?.resume()
        print("\(Self.self).\(#function): Connected to WebSocket at \(urlRequest)")
    }

    func disconnect() {
        webSocketTask?.cancel(with: .goingAway, reason: nil)
        webSocketTask = nil
        print("\(Self.self).\(#function): Disconnected from WebSocket")
    }

    func send(_ encodable: Encodable) async throws {
        guard let data = try? jsonEncoder.encode(encodable) else {
            throw WebSocketError.couldNotEncodeMessage
        }

        guard let webSocketTask else {
            throw WebSocketError.notConnected
        }
            
        try await webSocketTask.send(.data(data))
        print("\(Self.self).\(#function): Sent data message: \(data)")
    }

    func receiveNextMessage() async throws -> Data {
        guard let webSocketTask else {
            throw WebSocketError.notConnected
        }

        // Await a single message
        let message = try await webSocketTask.receive()

        // Return the message if it is of type `.data`
        if case let .data(data) = message {
            return data
        } else {
            throw WebSocketError.unexpectedMessageType
        }
    }

    // MARK: - Private methods

    private func receiveMessages(continuation: AsyncThrowingStream<Data, Error>.Continuation) async {
        guard let webSocketTask else {
            continuation.finish(throwing: WebSocketError.notConnected)
            return
        }

        do {
            while webSocketTask.state == .running {
                let message = try await webSocketTask.receive()
                if case let .data(data) = message {
                    continuation.yield(data)
                }
            }
            continuation.finish()
        } catch {
            continuation.finish(throwing: error)
        }
    }
}
