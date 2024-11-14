import Foundation

extension CrazyEights {
    public struct GameView: Decodable {
        public let cards: [Card]
        public let topOfPile: Card
        public let currentSuit: Card.Suit
        public let stockPileCount: Int
        public let discardPileCount: Int
        public let otherPlayers: [OtherPlayer]
    }
}
