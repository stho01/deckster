// swift-tools-version: 5.10

import PackageDescription

let package = Package(
    name: "Deckster",
    platforms: [.iOS(.v16), .macOS(.v14)],
    products: [
        .library(
            name: "Deckster",
            targets: ["Deckster"]
        ),
    ],
    targets: [
        .target(
            name: "Deckster",
            path: "iOS/Sources"
        ),
        .testTarget(
            name: "DecksterTests",
            dependencies: ["Deckster"],
            path: "iOS/Tests"
        ),
    ]
)
