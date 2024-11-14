import Foundation

extension Uno {
    public struct OtherPlayer: Decodable, Identifiable {
        public let id: String
        public let name: String
        public let numberOfCards: Int
    }
}
