import Foundation

public final class LoginClient {

    // MARK: - Private properties

    private let hostname: String

    private var urlString: String {
        "http://\(hostname)/login"
    }

    // MARK: - Init

    public init(hostname: String) {
        self.hostname = hostname
    }

    // MARK: - Login with async/await

    public func login(username: String, password: String) async throws -> UserModel {
        guard let url = URL(string: urlString) else {
            throw LoginServiceError.invalidURL(urlString)
        }

        let request = createRequest(url: url, username: username, password: password)
        let (data, response) = try await URLSession.shared.data(for: request)

        guard let httpResponse = response as? HTTPURLResponse, httpResponse.statusCode == 200 else {
            throw URLError(.badServerResponse)
        }

        return try JSONDecoder().decode(UserModel.self, from: data)
    }

    // MARK: - Private methods

    private func createRequest(url: URL, username: String, password: String) -> URLRequest {
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")

        let body = ["username": username, "password": password]
        request.httpBody = try? JSONSerialization.data(withJSONObject: body)

        return request
    }
}
