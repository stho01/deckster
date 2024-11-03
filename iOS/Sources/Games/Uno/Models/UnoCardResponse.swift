import Foundation

public struct UnoCardResponse: Decodable, Hashable {
    public let card: UnoCard

    public init(card: UnoCard) {
        self.card = card
    }
}
