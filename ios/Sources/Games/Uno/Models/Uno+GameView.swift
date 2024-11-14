import Foundation

extension Uno {
    public struct GameView: Decodable {
        public let cards: [Card]
        public let topOfPile: Card
        public let currentColor: Card.Color
        public let stockPileCount: Int
        public let discardPileCount: Int
        public let otherPlayers: [OtherPlayer]

        private enum CodingKeys: String, CodingKey {
            case cards
            case topOfPile
            case currentColor
            case stockPileCount
            case discardPileCount
            case otherPlayers
        }
    }
}
