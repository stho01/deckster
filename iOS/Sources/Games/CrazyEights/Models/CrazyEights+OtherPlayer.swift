import Foundation

extension CrazyEights {
    public struct OtherPlayer: Decodable, Identifiable {
        public let id: String
        public let name: String
        public let numberOfCards: Int

        enum CodingKeys: String, CodingKey {
            case id = "playerId"
            case name
            case numberOfCards
        }
    }
}
