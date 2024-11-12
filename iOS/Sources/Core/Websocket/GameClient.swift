import Foundation

enum GameClientError: Error {
    case invalidHandshakeResponse
    case invalidUrl(String)
}

final class GameClient<Action: Encodable, ActionResponse: Decodable, Notification: Decodable> {

    // MARK: - Internal properties

    var notificationStream: AsyncThrowingStream<Notification, Error> {
        AsyncThrowingStream { continuation in
            guard let notificationSocket = notificationSocket else {
                continuation.finish(throwing: WebSocketError.notConnected)
                return
            }

            Task {
                do {
                    for try await data in notificationSocket.messageStream {
                        do {
                            let decodedMessage = try decoder.decode(Notification.self, from: data)
                            continuation.yield(decodedMessage)
                        } catch {
                            continuation.finish(throwing: error)
                        }
                    }
                    continuation.finish()
                } catch {
                    continuation.finish(throwing: error)
                }
            }
        }
    }

    // MARK: - Private properties

    private let hostname: String
    private let gameName: String
    private let gameId: String
    private let accessToken: String
    private let decoder = JSONDecoder()
    private let actionSocket: WebSocketConnection
    private var notificationSocket: WebSocketConnection?

    // MARK: - Init

    init(hostname: String, gameName: String, gameId: String, accessToken: String) throws {
        self.hostname = hostname
        self.gameName = gameName
        self.gameId = gameId
        self.accessToken = accessToken

        let urlString = "ws://\(hostname)/\(gameName)/join/\(gameId)"
        let urlRequest = try Self.createUrlRequest(urlString: urlString, accessToken: accessToken)
        self.actionSocket = WebSocketConnection(urlRequest: urlRequest)
    }

    // MARK: - Internal methods

    func connect() async throws {
        actionSocket.connect()

        for try await data in actionSocket.messageStream {
            if let identifier = extractIdentifier(from: data) {
                try openNotificationSocket(with: identifier)

                // Stop listening to primary for initial identifier
                break
            } else {
                throw GameClientError.invalidHandshakeResponse
            }
        }
    }

    func disconnect() {
        actionSocket.disconnect()
        notificationSocket?.disconnect()
    }

    func sendAndReceive(_ action: Action) async throws -> ActionResponse {
        try await actionSocket.send(action)

        let data = try await actionSocket.receiveNextMessage()
        return try decoder.decode(ActionResponse.self, from: data)
    }

    // MARK: - Private methods

    private func openNotificationSocket(with identifier: String) throws {
        let urlString = "ws://\(hostname)/\(gameName)/join/\(identifier)/finish"
        let urlRequest = try Self.createUrlRequest(urlString: urlString, accessToken: accessToken)
        let connection = WebSocketConnection(urlRequest: urlRequest)
        connection.connect()
        self.notificationSocket = connection
        print("Secondary WebSocket connected with identifier: \(identifier)")
    }

    private func extractIdentifier(from data: Data) -> String? {
        do {
            let response = try decoder.decode(HandshakeResponse.self, from: data)
            return response.connectionId
        } catch {
            print("Failed to decode identifier: \(error)")
            return nil
        }
    }

    private  static func createUrlRequest(urlString: String, accessToken: String) throws -> URLRequest {
        guard let url = URL(string: urlString) else {
            throw GameClientError.invalidUrl(urlString)
        }
        let session = URLSession(configuration: .default)

        var request = URLRequest(url: url)
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        return request
    }
}
