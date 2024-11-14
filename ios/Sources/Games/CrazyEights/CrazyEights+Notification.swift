import Foundation

extension CrazyEights {
    public enum Notification: Decodable {
        case gameEnded(players: [Player])
        case gameStarted(gameId: String, viewOfGame: GameView)
        case itsYourTurn(viewOfGame: GameView)
        case playerDrewCard(playerId: String)
        case playerIsDone(playerId: String)
        case playerPassed(playerId: String)
        case playerPutCard(playerId: String, card: Card)
        case playerPutEight(playerId: String, card: Card, newSuit: Card.Suit)

        enum CodingKeys: String, CodingKey {
            case kind = "type"
        }

        public init(from decoder: any Decoder) throws {
            let container = try decoder.container(keyedBy: CodingKeys.self)
            let kind = try container.decode(Kind.self, forKey: .kind)

            switch kind {
            case .gameEnded:
                let model = try GameEnded(from: decoder)
                self = .gameEnded(players: model.players)
            case .gameStarted:
                let model = try GameStarted(from: decoder)
                self = .gameStarted(gameId: model.gameId, viewOfGame: model.playerViewOfGame)
            case .itsYourTurn:
                let model = try ItsYourTurn(from: decoder)
                self = .itsYourTurn(viewOfGame: model.playerViewOfGame)
            case .playerDrewCard:
                let model = try PlayerDrewCard(from: decoder)
                self = .playerDrewCard(playerId: model.playerId)
            case .playerIsDone:
                let model = try PlayerIsDone(from: decoder)
                self = .playerIsDone(playerId: model.playerId)
            case .playerPassed:
                let model = try PlayerPassed(from: decoder)
                self = .playerPassed(playerId: model.playerId)
            case .playerPutCard:
                let model = try PlayerPutCard(from: decoder)
                self = .playerPutCard(playerId: model.playerId, card: model.card)
            case .playerPutEight:
                let model = try PlayerPutEight(from: decoder)
                self = .playerPutEight(
                    playerId: model.playerId,
                    card: model.card,
                    newSuit: model.newSuit
                )
            }
        }
    }
}

// MARK: - Notification kind

extension CrazyEights.Notification {
    enum Kind: String, Decodable {
        case gameEnded = "CrazyEights.GameEndedNotification"
        case gameStarted = "CrazyEights.GameStartedNotification"
        case itsYourTurn = "CrazyEights.ItsYourTurnNotification"
        case playerDrewCard = "CrazyEights.PlayerDrewCardNotification"
        case playerIsDone = "CrazyEights.PlayerIsDoneNotification"
        case playerPassed = "CrazyEights.PlayerPassedNotification"
        case playerPutCard = "CrazyEights.PlayerPutCardNotification"
        case playerPutEight = "CrazyEights.PlayerPutEightNotification"
    }
}

// MARK: - Models

extension CrazyEights.Notification {
    struct GameEnded: Decodable {
        let players: [Player]
    }

    struct GameStarted: Decodable {
        let gameId: String
        let playerViewOfGame: CrazyEights.GameView
    }

    struct ItsYourTurn: Decodable {
        let playerViewOfGame: CrazyEights.GameView
    }

    struct PlayerDrewCard: Decodable {
        let playerId: String
    }

    struct PlayerIsDone: Decodable {
        let playerId: String
    }

    struct PlayerPassed: Decodable {
        let playerId: String
    }

    struct PlayerPutCard: Decodable {
        let playerId: String
        let card: Card
    }

    struct PlayerPutEight: Decodable {
        let playerId: String
        let card: Card
        let newSuit: Card.Suit
    }
}
