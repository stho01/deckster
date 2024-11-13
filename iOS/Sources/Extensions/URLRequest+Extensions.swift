import Foundation

public enum URLRequestError: Error {
    case invalidUrl(String)
}

extension URLRequest {
    static func create(_ urlString: String, method: String = "GET", accessToken: String) throws -> URLRequest {
        guard let url = URL(string: urlString) else {
            throw URLRequestError.invalidUrl(urlString)
        }

        var request = URLRequest(url: url)
        request.httpMethod = method
        request.addValue("application/json", forHTTPHeaderField: "Content-Type")
        request.addValue("Bearer \(accessToken)", forHTTPHeaderField: "Authorization")
        return request
    }
}
