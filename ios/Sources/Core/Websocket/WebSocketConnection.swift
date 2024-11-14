import Foundation

final class WebSocketConnection {

    // MARK: - Internal properties

    private(set) var webSocketTask: URLSessionWebSocketTask?

    var messageStream: AsyncThrowingStream<Data, Error> {
        AsyncThrowingStream { continuation in
            Task {
                await self.receiveMessages(continuation: continuation)
            }
        }
    }

    // MARK: - Private properties

    private let urlRequest: URLRequest
    private let urlSession: URLSession
    private let jsonEncoder = JSONEncoder()

    // MARK: - Init

    init(urlRequest: URLRequest, urlSession: URLSession = .shared) {
        self.urlRequest = urlRequest
        self.urlSession = urlSession
    }

    // MARK: - Internal methods

    func connect() {
        guard webSocketTask == nil else { return }
        webSocketTask = urlSession.webSocketTask(with: urlRequest)
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
        switch message {
        case .data(let data):
            return data
        case .string(let string):
            return Data(string.utf8)
        }
    }

    func receiveMessages(continuation: AsyncThrowingStream<Data, Error>.Continuation) async {
        guard let webSocketTask else {
            continuation.finish(throwing: WebSocketError.notConnected)
            return
        }

        do {
            while webSocketTask.state == .running {
                print("listening!")
                let message = try await webSocketTask.receive()
                switch message {
                case .data(let data):
                    print(String(data: data, encoding: .utf8)!)
                    continuation.yield(data)
                case .string(let string):
                    print(string)
                    let data = Data(string.utf8)
                    continuation.yield(data)
                }
            }
            continuation.finish()
        } catch {
            continuation.finish(throwing: error)
        }
    }
}
