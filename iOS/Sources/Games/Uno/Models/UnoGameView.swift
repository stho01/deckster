import Foundation

public struct UnoGameView: Decodable {
    public let cards: [UnoCard]
    public let topOfPile: UnoCard
    public let currentColor: UnoCard.Color
    public let stockPileCount: Int
    public let discardPileCount: Int
    public let otherPlayers: [UnoPlayer]

    private enum CodingKeys: String, CodingKey {
        case cards
        case topOfPile
        case currentColor
        case stockPileCount
        case discardPileCount
        case otherPlayers
    }
}
