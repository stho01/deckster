import Foundation

public final class LoginService {

    // MARK: - Public properties

    public weak var delegate: LoginServiceDelegate?

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

    public func loginAsync(username: String, password: String) async throws -> UserModel {
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

    // MARK: - Login with delegate

    public func loginWithDelegate(username: String, password: String) {
        guard let url = URL(string: urlString) else {
            delegate?.didFailToLogin(error: LoginServiceError.invalidURL(urlString))
            return
        }

        let request = createRequest(url: url, username: username, password: password)
        let dataTask = URLSession.shared.dataTask(with: request) { [weak self] data, response, error in
            if let error {
                self?.delegate?.didFailToLogin(error: error)
                return
            }

            guard
                let data = data,
                let httpResponse = response as? HTTPURLResponse,
                httpResponse.statusCode == 200
            else {
                self?.delegate?.didFailToLogin(error: URLError(.badServerResponse))
                return
            }

            do {
                let userModel = try JSONDecoder().decode(UserModel.self, from: data)
                self?.delegate?.didLoginSuccessfully(userModel: userModel)
            } catch {
                self?.delegate?.didFailToLogin(error: error)
            }
        }

        dataTask.resume()
    }

    // MARK: - Private methods

    private func createUrl() -> URL? {
        URL(string: "https://\(hostname)/login")
    }

    private func createRequest(url: URL, username: String, password: String) -> URLRequest {
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")

        let body = ["username": username, "password": password]
        request.httpBody = try? JSONSerialization.data(withJSONObject: body)

        return request
    }
}
