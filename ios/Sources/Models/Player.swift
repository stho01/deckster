import Foundation

public struct Player: Decodable, Identifiable {
    public let id: String
    public let name: String
    public let points: Int
    public let cardsInHand: Int
    public let interestingFacts: [String: String]
}
