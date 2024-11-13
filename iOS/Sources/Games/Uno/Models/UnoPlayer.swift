import Foundation

public struct UnoPlayer: Decodable, Identifiable {
    public let id: String
    public let name: String
    public let numberOfCards: Int
}
