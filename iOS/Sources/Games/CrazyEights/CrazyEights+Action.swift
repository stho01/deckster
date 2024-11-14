import Foundation

extension CrazyEights {
    public enum Action: Encodable {
        case putCard(card: Card)
        case putEight(card: Card, newSuit: Card.Suit)
        case drawCard
        case pass

        public func encode(to encoder: any Encoder) throws {
            var container = encoder.singleValueContainer()
            switch self {
            case .putCard(let card):
                try container.encode(PutCard(card: card))
            case .putEight(let card, let newSuit):
                try container.encode(PutEight(card: card, newSuit: newSuit))
            case .drawCard:
                try container.encode(DrawCard())
            case .pass:
                try container.encode(Pass())
            }
        }
    }
}

// MARK: - Models

extension CrazyEights.Action {
    struct PutCard: Encodable {
        let type = "CrazyEights.PutCardRequest"
        let card: Card
    }

    struct PutEight: Encodable {
        let type = "CrazyEights.PutEightRequest"
        let card: Card
        let newSuit: Card.Suit
    }

    struct DrawCard: Encodable {
        let type = "CrazyEights.DrawCardRequest"
    }

    struct Pass: Encodable {
        let type = "CrazyEights.PassRequest"
    }
}
