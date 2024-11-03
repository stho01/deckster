import Foundation

public struct UnoCard: Decodable, Hashable {
    public let value: UnoValue
    public let color: String

    public init(value: UnoValue, color: String) {
        self.value = value
        self.color = color
    }
}

extension UnoCard {
    public enum UnoValue: Int, Decodable {
        case zero = 0
        case one = 1
        case two = 2
        case three = 3
        case four = 4
        case five = 5
        case six = 6
        case seven = 7
        case eight = 8
        case nine = 9
        case skip = 21
        case reverse = 22
        case drawTwo = 23
        case wild = 51
        case wildDrawFour = 52
    }

    public enum Color: String, Decodable {
        case red
        case yellow
        case green
        case blue
        case wild
    }
}
