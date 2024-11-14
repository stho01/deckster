import Foundation

extension Uno {
    public enum Notification: Decodable {
        case gameEnded(players: [Player])
        case gameStarted(gameId: String, gameView: GameView)
        case itsYourTurn(gameView: GameView)
        case playerDrewCard(playerId: String)
        case playerPassed(playerId: String)
        case playerPutCard(playerId: String, card: Card)
        case playerPutWild(playerId: String, card: Card, newColor: Card.Color)
        case roundEnded(players: [Player])
        case roundStarted(gameView: GameView)

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
                self = .gameStarted(gameId: model.gameId, gameView: model.playerGameOfView)
            case .itsYourTurn:
                let model = try ItsYourTurn(from: decoder)
                self = .itsYourTurn(gameView: model.playerGameOfView)
            case .playerDrewCard:
                let model = try PlayerDrewCard(from: decoder)
                self = .playerDrewCard(playerId: model.playerId)
            case .playerPassed:
                let model = try PlayerPassed(from: decoder)
                self = .playerPassed(playerId: model.playerId)
            case .playerPutCard:
                let model = try PlayerPutCard(from: decoder)
                self = .playerPutCard(playerId: model.playerId, card: model.card)
            case .playerPutWild:
                let model = try PlayerPutWild(from: decoder)
                self = .playerPutWild(playerId: model.playerId, card: model.card, newColor: model.newColor)
            case .roundEnded:
                let model = try RoundEnded(from: decoder)
                self = .roundEnded(players: model.players)
            case .roundStarted:
                let model = try RoundStarted(from: decoder)
                self = .roundStarted(gameView: model.playerGameOfView)
            }
        }
    }
}

// MARK: - Notification kind

extension Uno.Notification {
    enum Kind: String, Decodable {
        case gameEnded = "Uno.GameEndedNotification"
        case gameStarted = "Uno.GameStartedNotification"
        case itsYourTurn = "Uno.ItsYourTurnNotification"
        case playerDrewCard = "Uno.PlayerDrewCardNotification"
        case playerPassed = "Uno.PlayerPassedNotification"
        case playerPutCard = "Uno.PlayerPutCardNotification"
        case playerPutWild = "Uno.PlayerPutWildNotification"
        case roundEnded = "Uno.RoundEndedNotification"
        case roundStarted = "Uno.RoundStartedNotification"
    }
}

// MARK: - Models

extension Uno.Notification {
    struct PlayerPutCard: Decodable {
        let playerId: String
        let card: Uno.Card
    }

    struct PlayerPutWild: Decodable {
        let playerId: String
        let card: Uno.Card
        let newColor: Uno.Card.Color
    }

    struct PlayerDrewCard: Decodable {
        let playerId: String
    }

    struct PlayerPassed: Decodable {
        let playerId: String
    }

    struct ItsYourTurn: Decodable {
        let playerGameOfView: Uno.GameView
    }

    struct GameStarted: Decodable {
        let gameId: String
        let playerGameOfView: Uno.GameView
    }

    struct GameEnded: Decodable {
        let players: [Player]
    }

    struct RoundStarted: Decodable {
        let playerGameOfView: Uno.GameView
    }

    struct RoundEnded: Decodable {
        let players: [Player]
    }
}
