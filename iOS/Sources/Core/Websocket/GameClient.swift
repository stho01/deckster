import Foundation

enum GameClientError: Error {
    case invalidHandshakeResponse
    case invalidUrl(String)
}

public class GameClient<Action: Encodable, ActionResponse: Decodable, Notification: Decodable> {

    // MARK: - Public properties

    public var notificationStream: AsyncThrowingStream<Notification, Error> {
        AsyncThrowingStream { continuation in
            guard let notificationSocket else {
                continuation.finish(throwing: WebSocketError.notConnected)
                return
            }

            Task {
                do {
                    for try await data in notificationSocket.messageStream {
                        let decodedMessage = try decoder.decode(Notification.self, from: data)
                        continuation.yield(decodedMessage)
                    }
                    continuation.finish()
                } catch {
                    continuation.finish(throwing: error)
                }
            }
        }
    }

    // MARK: - Internal properties

    let hostname: String
    let gameName: String
    let gameId: String
    let accessToken: String
    private(set) var isConnected = false

    // MARK: - Private properties

    private let urlSession: URLSession
    private let decoder = JSONDecoder()
    private let actionSocket: WebSocketConnection
    private var notificationSocket: WebSocketConnection?

    // MARK: - Init

    init(
        hostname: String,
        gameName: String,
        gameId: String,
        accessToken: String,
        urlSession: URLSession = .shared
    ) throws {
        self.hostname = hostname
        self.gameName = gameName
        self.gameId = gameId
        self.accessToken = accessToken
        self.urlSession = urlSession

        let urlString = "ws://\(hostname)/\(gameName)/join/\(gameId)"
        let urlRequest = try URLRequest.create(urlString, accessToken: accessToken)
        self.actionSocket = WebSocketConnection(urlRequest: urlRequest, urlSession: urlSession)
    }

    // MARK: - Public methods

    public func startGame() async throws {
        let urlString = "http://\(hostname)/\(gameName)/games/\(gameId)/start"
        let urlRequest = try URLRequest.create(urlString, accessToken: accessToken)
        let (_, _) = try await urlSession.data(for: urlRequest)
        print("Game started!")
    }

    public func connect() async throws {
        guard !isConnected else { return }
        actionSocket.connect()

        let data = try await actionSocket.receiveNextMessage()
        if let identifier = extractIdentifier(from: data) {
            print(identifier)
            try await openNotificationSocket(with: identifier)

            isConnected = true
        } else {
            throw GameClientError.invalidHandshakeResponse
        }
    }

    public func disconnect() {
        actionSocket.disconnect()
        notificationSocket?.disconnect()
    }

    public func sendAction(_ action: Action) async throws -> ActionResponse {
        async let data = actionSocket.receiveNextMessage()
        try await actionSocket.send(action)

        return try decoder.decode(ActionResponse.self, from: await data)
    }

    // MARK: - Private methods

    private func openNotificationSocket(with identifier: String) async throws {
        let urlString = "ws://\(hostname)/\(gameName)/join/\(identifier)/finish"
        let urlRequest = try URLRequest.create(urlString, accessToken: accessToken)
        let connection = WebSocketConnection(urlRequest: urlRequest)
        connection.connect()
        self.notificationSocket = connection

        let data = try await connection.receiveNextMessage()
        _ = try decoder.decode(NotificationSocketHandshake.self, from: data)
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
}
