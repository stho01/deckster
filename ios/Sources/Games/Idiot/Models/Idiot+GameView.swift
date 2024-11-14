import Foundation

extension Idiot {
    public struct GameView: Decodable {
        public let cardsOnHand: [Card]
        public let cardsFacingUp: [Card]
        public let stockPileCount: Int
        public let otherPlayers: [OtherPlayer]
        public let cardsFacingDownCount: Int
    }
}
