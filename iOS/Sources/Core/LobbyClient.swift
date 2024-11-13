import Foundation

public final class LobbyClient {
    private let hostname: String
    private let accessToken: String
    private let urlSession: URLSession

    public init(
        hostname: String,
        accessToken: String,
        urlSession: URLSession = .shared
    ) {
        self.hostname = hostname
        self.accessToken = accessToken
        self.urlSession = urlSession
    }

    public func createGame(game: Endpoint, name: String? = nil) async throws -> GameCreated {
        let urlString = "http://\(hostname)/\(game.rawValue)/create" + (name.map { "/\($0)" } ?? "")
        let urlRequest = try URLRequest.create(urlString, method: "POST", accessToken: accessToken)
        let (data, _) = try await urlSession.data(for: urlRequest)
        return try JSONDecoder().decode(GameCreated.self, from: data)
    }
}

public struct GameCreated: Decodable {
    public let id: String
}
