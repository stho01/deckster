import Foundation

extension Idiot {
    public enum Notification: Decodable {
        case discardPileFlushed(playerId: String)
        case gameEnded
        case gameStarted
        case itsTimeToSwapCards(gameView: Idiot.GameView)
        case itsYourTurn
        case playerAttemptedPuttingCard(playerId: String, card: Card)
        case playerDrewCards(playerId: String, numberOfCards: Int)
        case playerIsDone(playerId: String)
        case playerIsReady(playerId: String)
        case playerPulledInDiscardPile(playerId: String)
        case playerPutCards(playerId: String, cards: [Card])
        case playerSwappedCards(playerId: String, cardNowOnHand: Card, cardNowFacingUp: Card)

        enum CodingKeys: String, CodingKey {
            case kind = "type"
        }

        public init(from decoder: any Decoder) throws {
            let container = try decoder.container(keyedBy: CodingKeys.self)
            let kind = try container.decode(Kind.self, forKey: .kind)

            switch kind {
            case .discardPileFlushed:
                let model = try DiscardPileFlushed(from: decoder)
                self = .discardPileFlushed(playerId: model.playerId)
            case .gameEnded:
                let model = try GameEnded(from: decoder)
                self = .gameEnded
            case .gameStarted:
                let model = try GameStarted(from: decoder)
                self = .gameStarted
            case .itsTimeToSwapCards:
                let model = try ItsTimeToSwapCards(from: decoder)
                self = .itsTimeToSwapCards(gameView: model.playerViewOfGame)
            case .itsYourTurn:
                let model = try ItsYourTurn(from: decoder)
                self = .itsYourTurn
            case .playerAttemptedPuttingCard:
                let model = try PlayerAttemptedPuttingCard(from: decoder)
                self = .playerAttemptedPuttingCard(playerId: model.playerId, card: model.card)
            case .playerDrewCards:
                let model = try PlayerDrewCards(from: decoder)
                self = .playerDrewCards(
                    playerId: model.playerId,
                    numberOfCards: model.numberOfCards
                )
            case .playerIsDone:
                let model = try PlayerIsDone(from: decoder)
                self = .playerIsDone(playerId: model.playerId)
            case .playerIsReady:
                let model = try PlayerIsReady(from: decoder)
                self = .playerIsReady(playerId: model.playerId)
            case .playerPulledInDiscardPile:
                let model = try PlayerPulledInDiscardPile(from: decoder)
                self = .playerPulledInDiscardPile(playerId: model.playerId)
            case .playerPutCards:
                let model = try PlayerPutCards(from: decoder)
                self = .playerPutCards(playerId: model.playerId, cards: model.cards)
            case .playerSwappedCards:
                let model = try PlayerSwappedCards(from: decoder)
                self = .playerSwappedCards(
                    playerId: model.playerId,
                    cardNowOnHand: model.cardNowOnHand,
                    cardNowFacingUp: model.cardNowFacingUp
                )
            }
        }
    }
}

// MARK: - Notification kind

extension Idiot.Notification {
    enum Kind: String, Decodable {
        case discardPileFlushed = "Idiot.DiscardPileFlushedNotification"
        case gameEnded = "Idiot.GameEndedNotification"
        case gameStarted = "Idiot.GameStartedNotification"
        case itsTimeToSwapCards = "Idiot.ItsTimeToSwapCardsNotification"
        case itsYourTurn = "Idiot.ItsYourTurnNotification"
        case playerAttemptedPuttingCard = "Idiot.PlayerAttemptedPuttingCardNotification"
        case playerDrewCards = "Idiot.PlayerDrewCardsNotification"
        case playerIsDone = "Idiot.PlayerIsDoneNotification"
        case playerIsReady = "Idiot.PlayerIsReadyNotification"
        case playerPulledInDiscardPile = "Idiot.PlayerPulledInDiscardPileNotification"
        case playerPutCards = "Idiot.PlayerPutCardsNotification"
        case playerSwappedCards = "Idiot.PlayerSwappedCardsNotification"
    }
}

// MARK: - Models

extension Idiot.Notification {
    struct PlayerSwappedCards: Decodable {
        let playerId: String
        let cardNowOnHand: Card
        let cardNowFacingUp: Card
    }

    struct PlayerPutCards: Decodable {
        let playerId: String
        let cards: [Card]
    }

    struct PlayerIsReady: Decodable {
        let playerId: String
    }

    struct PlayerIsDone: Decodable {
        let playerId: String
    }

    struct DiscardPileFlushed: Decodable {
        let playerId: String
    }

    struct ItsYourTurn: Decodable {}

    struct PlayerDrewCards: Decodable {
        let playerId: String
        let numberOfCards: Int
    }

    struct PlayerAttemptedPuttingCard: Decodable {
        let playerId: String
        let card: Card
    }

    struct PlayerPulledInDiscardPile: Decodable {
        let playerId: String
    }

    struct GameStarted: Decodable {}

    struct GameEnded: Decodable {}

    struct ItsTimeToSwapCards: Decodable {
        let playerViewOfGame: Idiot.GameView
    }
}
