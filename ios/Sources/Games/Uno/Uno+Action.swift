import Foundation

extension Uno {
    public enum Action: Encodable {
        case putCard(card: Card)
        case putWild(card: Card, newColor: Card.Color)
        case drawCard
        case pass

        public func encode(to encoder: any Encoder) throws {
            var container = encoder.singleValueContainer()
            switch self {
            case .putCard(let card):
                try container.encode(PutCard(card: card))
            case .putWild(let card, let newColor):
                try container.encode(PutWild(card: card, newColor: newColor))
            case .drawCard:
                try container.encode(DrawCard())
            case .pass:
                try container.encode(Pass())
            }
        }
    }
}

// MARK: - Models

extension Uno.Action {
    struct PutCard: Encodable {
        let type = "Uno.PutCardRequest"
        let card: Uno.Card
    }

    struct PutWild: Encodable {
        let type = "Uno.PutWildRequest"
        let card: Uno.Card
        let newColor: Uno.Card.Color
    }

    struct DrawCard: Encodable {
        let type = "Uno.DrawCardRequest"
    }

    struct Pass: Encodable {
        let type = "Uno.PassRequest"
    }
}
