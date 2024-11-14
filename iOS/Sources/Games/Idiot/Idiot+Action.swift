import Foundation

extension Idiot {
    public enum Action: Encodable {
        case iamReady
        case swapCards(cardOnHand: Card, cardFacingUp: Card)
        case putCardsFromHand(cards: [Card])
        case putCardsFacingUp(cards: [Card])
        case putCardFacingDown(index: Int)
        case drawCards(numberOfCards: Int)
        case pullInDiscardPile
        case putChanceCard

        public func encode(to encoder: any Encoder) throws {
            var container = encoder.singleValueContainer()
            switch self {
            case .iamReady:
                try container.encode(IamReady())
            case .swapCards(let cardOnHand, let cardFacingUp):
                try container.encode(SwapCards(cardOnHand: cardOnHand, cardFacingUp: cardFacingUp))
            case .putCardsFromHand(let cards):
                try container.encode(PutCardsFromHand(cards: cards))
            case .putCardsFacingUp(let cards):
                try container.encode(PutCardsFacingUp(cards: cards))
            case .putCardFacingDown(let index):
                try container.encode(PutCardFacingDown(index: index))
            case .drawCards(let numberOfCards):
                try container.encode(DrawCards(numberOfCards: numberOfCards))
            case .pullInDiscardPile:
                try container.encode(PullInDiscardPile())
            case .putChanceCard:
                try container.encode(PutChanceCard())
            }
        }
    }
}

// MARK: - Models

extension Idiot.Action {
    struct IamReady: Encodable {
        let type = "Idiot.IamReadyRequest"
    }

    struct SwapCards: Encodable {
        let type = "Idiot.SwapCardsRequest"
        let cardOnHand: Card
        let cardFacingUp: Card
    }

    struct PutCardsFromHand: Encodable {
        let type = "Idiot.PutCardsFromHandRequest"
        let cards: [Card]
    }

    struct PutCardsFacingUp: Encodable {
        let type = "Idiot.PutCardsFacingUpRequest"
        let cards: [Card]
    }

    struct PutCardFacingDown: Encodable {
        let type = "Idiot.PutCardFacingDownRequest"
        let index: Int
    }

    struct DrawCards: Encodable {
        let type = "Idiot.DrawCardsRequest"
        let numberOfCards: Int
    }

    struct PullInDiscardPile: Encodable {
        let type = "Idiot.PullInDiscardPileRequest"
    }

    struct PutChanceCard: Encodable {
        let type = "Idiot.PutChanceCardRequest"
    }
}
