import Foundation

extension Idiot {
    public struct OtherPlayer: Decodable, Identifiable {
        public let id: String
        public let name: String
        public let cardsOnHandCount: Int
        public let cardsFacingUp: [Card]
        public let cardsFacingDownCount: Int
    }
}
