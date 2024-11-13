// swift-tools-version: 5.10

import PackageDescription

let package = Package(
    name: "GameSocket",
    platforms: [.iOS(.v16), .macOS(.v14)],
    products: [
        .library(
            name: "GameSocket",
            targets: ["GameSocket"]
        ),
    ],
    targets: [
        .target(
            name: "GameSocket",
            path: "iOS/Sources"
        ),
        .testTarget(
            name: "GameSocketTests",
            dependencies: ["GameSocket"],
            path: "iOS/Tests"
        ),
    ]
)
